using System.CommandLine;
using System.CommandLine.Parsing;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVN;
#if WINDOWS
using Dbosoft.OVN.Windows;
#endif
using Dbosoft.OVNAgent;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var logLevelOptions = new System.CommandLine.Option<LogLevel>("--logLevel", () => LogLevel.Information);
var nodeTypeOptions = new System.CommandLine.Option<NodeType>("--nodes", () => NodeType.AllInOne );

var rootCommand = new RootCommand();
rootCommand.AddGlobalOption(logLevelOptions);

var runCommand = new Command("run", "Runs OVN and OVS locally.");
runCommand.AddOption(nodeTypeOptions);
runCommand.SetHandler(RunCommand, logLevelOptions, nodeTypeOptions);
rootCommand.Add(runCommand);

var netplanCommand = new Command("netplan", "netplan commands");
rootCommand.Add(netplanCommand);

var applyCommand = new Command("apply", "apply network plan");
var fileOption = new System.CommandLine.Option<FileInfo>("--file");
applyCommand.AddOption(fileOption);
applyCommand.SetHandler(ApplyNetplan, logLevelOptions, fileOption);
netplanCommand.AddCommand(applyCommand);

var serviceCommand = new Command("service", "service commands");
rootCommand.Add(serviceCommand);

#if WINDOWS

var installServiceCommand = new Command("install", "install OVN as service");
installServiceCommand.AddOption(nodeTypeOptions);
installServiceCommand.SetHandler((l, n) => ManageServiceCommand(true, l, n), logLevelOptions, nodeTypeOptions );
serviceCommand.AddCommand(installServiceCommand);

var removeServiceCommand = new Command("remove", "removes OVN service");
removeServiceCommand.AddOption(nodeTypeOptions);
removeServiceCommand.SetHandler((l, n) => ManageServiceCommand(false, l, n), logLevelOptions, nodeTypeOptions );

serviceCommand.AddCommand(removeServiceCommand);

var hyperVCommand = new Command("hyperv", "Hyper-V commands");
rootCommand.Add(hyperVCommand);

var adapterIdArgument = new Argument<string>("adapterId", "Hyper-V network adapter ID");
var portNameArgument = new Argument<string>("portName", "OVS port name");

var hyperVAdapterCommand = new Command("adapter", "Hyper-V network adapter commands");
hyperVCommand.AddCommand(hyperVAdapterCommand);

var hyperVAdapterGetCommand = new Command("get", "get network adapter ID");
hyperVAdapterCommand.AddCommand(hyperVAdapterGetCommand);
hyperVAdapterGetCommand.AddArgument(portNameArgument);
hyperVAdapterGetCommand.SetHandler(GetAdapterId, portNameArgument);

var hyperVPortNameCommand = new Command("portname", "Hyper-V OVS port name commands");
hyperVCommand.AddCommand(hyperVPortNameCommand);

var hyperVPortNameGetCommand = new Command("get", "get port name");
hyperVPortNameCommand.AddCommand(hyperVPortNameGetCommand);
hyperVPortNameGetCommand.AddArgument(adapterIdArgument);
hyperVPortNameGetCommand.SetHandler(GetPortName, adapterIdArgument);

var hyperVPortNameListCommand = new Command("list", "list port names");
hyperVPortNameCommand.AddCommand(hyperVPortNameListCommand);
hyperVPortNameListCommand.SetHandler(ListPortNames);

var hyperVPortNameSetCommand = new Command("set", "set port name");
hyperVPortNameCommand.AddCommand(hyperVPortNameSetCommand);
hyperVPortNameSetCommand.AddArgument(adapterIdArgument);
hyperVPortNameSetCommand.AddArgument(portNameArgument);
hyperVPortNameSetCommand.SetHandler(SetPortName, adapterIdArgument, portNameArgument);

#endif

if (args.Length > 0) 
    return await rootCommand.InvokeAsync(args);

// Extremely simple REPL environment
while (true)
{
    Console.Write("OVNAgent > ");
    string? cmd = Console.ReadLine();
    if (string.IsNullOrEmpty(cmd))
        return 0;
    
    await rootCommand.InvokeAsync(CommandLineStringSplitter.Instance.Split(cmd).ToArray());
}

static string BuildServiceName(NodeType nodeType)
{
    return $"OVN_{nodeType}".ToLowerInvariant();
}

static Task RunCommand(LogLevel logLevel, NodeType nodeType)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureHostOptions(cfg => cfg.ShutdownTimeout = TimeSpan.FromSeconds(15))
        .UseWindowsService(cfg => cfg.ServiceName = BuildServiceName(nodeType))
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddOVNCore(services);
            
            if (nodeType is NodeType.AllInOne or NodeType.OVNCentral or NodeType.OVNDB)
            {
                services.AddHostedNode<OVNDatabaseNode>();
            }

            if (nodeType is not (NodeType.AllInOne or NodeType.OVNCentral or NodeType.OVNController))
                return;
            
            services.AddHostedNode<NetworkControllerNode>();

            if (nodeType is not (NodeType.AllInOne or NodeType.Chassis))
                return;
             
            services.AddHostedNode<OVSDbNode>();
            services.AddHostedNode<OVSSwitchNode>();
            services.AddHostedNode<OVNChassisNode>();
        })
        .Build();

    return host.RunAsync();
}

static async Task<int> ApplyNetplan(LogLevel logLevel, FileInfo netplanFile)
{
    var serializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    using var yamlReader = netplanFile.OpenText();
    var networkPlanValues = serializer.Deserialize<IDictionary<object,object>>(yamlReader);
    var netplan = NetworkPlanParser.ParseYaml(networkPlanValues);

    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
    
            AddOVNCore(services);
            services.AddSingleton(
                sp => new NetworkPlanRealizer(
                    new OVNControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().NorthDBConnection),
                    sp.GetRequiredService<ILogger<NetworkPlanRealizer>>()));
        })
        .Build();

    var result = await host.Services.GetRequiredService<NetworkPlanRealizer>()
        .ApplyNetworkPlan(netplan);

    return result.Match(
        Right: _ => 0,
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(
                Error.New("Failed to apply network plan.", error)));
            return -1;
        });
}

#if WINDOWS

static async Task ManageServiceCommand(bool install, LogLevel logLevel, NodeType nodeType)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(AddOVNCore)
        .Build();

    var serviceManager = host.Services.GetRequiredService<ISystemEnvironment>().GetServiceManager(
        BuildServiceName(nodeType));

    var command = $"\"{Environment.ProcessPath}\" run --nodes {nodeType} ";
    
    if (install)
    {
        _ = await serviceManager.CreateService(
                $"OVN {nodeType}", command, Seq<string>.Empty, CancellationToken.None)
            .Bind(_ => serviceManager.EnsureServiceStarted(CancellationToken.None))
            .IfLeft(l => l.Throw());
        return;
    }
    
    _ = await serviceManager.EnsureServiceStopped(CancellationToken.None)
        .Bind(_ => serviceManager.RemoveService(CancellationToken.None))
        .IfLeft(l => l.Throw());
}

static async Task<int> GetAdapterId(string portName)
{
    using var portManager = new HyperVOvsPortManager();
    var result = await portManager.GetAdapterIds(portName);

    return result.Match(
        Right: adapterIds =>
        {
            if (adapterIds.Length > 1)
            {
                Console.WriteLine($"Multiple adapters found for port name '{portName}'.");
                return -1;
            }

            adapterIds.Iter(p => Console.WriteLine(p));
            return 0;
        },
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(error));
            return -1;
        });
}

static async Task<int> GetPortName(string adapterId)
{
    using var portManager = new HyperVOvsPortManager();
    var result = await portManager.GetPortName(adapterId);

    return result.Match(
        Right: portName =>
        {
            Console.WriteLine(portName);
            return 0;
        },
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(error));
            return -1;
        });
}

static async Task<int> ListPortNames()
{
    using var portManager = new HyperVOvsPortManager();
    var result = await portManager.GetPortNames();

    return result.Match(
        Right: portNames =>
        {
            portNames.Iter(p => Console.WriteLine($"{p.AdapterId} - {p.PortName}"));
            return 0;
        },
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(error));
            return -1;
        });
}


static async Task<int> SetPortName(string adapterId, string portName)
{
    using var portManager = new HyperVOvsPortManager();
    var result = await portManager.SetPortName(adapterId, portName);

    return result.Match(
        Right: _ => 0,
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(error));
            return -1;
        });
}

#endif

static void AddOVNCore(IServiceCollection services)
{
#if WINDOWS
    services.AddSingleton<ISystemEnvironment, WindowsSystemEnvironment>();
#else
    services.AddSingleton<ISystemEnvironment, SystemEnvironment>();
#endif
    services.AddSingleton<IOvsSettings, LocalOVSWithOVNSettings>();
    services.AddSingleton<IOVNSettings, LocalOVSWithOVNSettings>();
}

/// <summary>
/// Type of started node(s)
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Run all OVN nodes
    /// </summary>
    AllInOne,
    
    /// <summary>
    /// Run only chassis node
    /// </summary>
    Chassis,
    
    /// <summary>
    /// Run all nodes except the chassis node
    /// </summary>
    OVNCentral,
    
    /// <summary>
    /// Run OVN database node
    /// </summary>
    OVNDB,
    
    /// <summary>
    /// RUn OVN controller
    /// </summary>
    OVNController
}
