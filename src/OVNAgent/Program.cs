using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
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
var fileOption = new System.CommandLine.Option<FileInfo>("--file");

var rootCommand = new RootCommand();
rootCommand.AddGlobalOption(logLevelOptions);

var runCommand = new Command("run", "Runs OVN and OVS locally.");
runCommand.AddOption(nodeTypeOptions);
runCommand.SetHandler(RunCommand, logLevelOptions, nodeTypeOptions);
rootCommand.Add(runCommand);

var runPrimaryCommand = new Command("run-primary", "Runs OVN primary.");
runPrimaryCommand.SetHandler(RunPrimaryCommand, logLevelOptions);
rootCommand.Add(runPrimaryCommand);

var runSecondaryCommand = new Command("run-secondary", "Runs OVN secondary.");
runSecondaryCommand.SetHandler(RunSecondaryCommand, logLevelOptions);
rootCommand.Add(runSecondaryCommand);


var netplanCommand = new Command("netplan", "netplan commands");
rootCommand.Add(netplanCommand);

var netplanApplyCommand = new Command("apply", "apply network plan");
netplanApplyCommand.AddOption(fileOption);
netplanApplyCommand.SetHandler(ApplyNetplan, logLevelOptions, fileOption);
netplanCommand.AddCommand(netplanApplyCommand);

var clusterPlanCommand = new Command("clusterplan", "cluster plan commands");
rootCommand.Add(clusterPlanCommand);

var clusterPlanApplyCommand = new Command("apply", "apply cluster plan");
clusterPlanApplyCommand.AddOption(fileOption);
clusterPlanApplyCommand.SetHandler(ApplyClusterPlan, logLevelOptions, fileOption);
clusterPlanCommand.AddCommand(clusterPlanApplyCommand);

var chassisPlanCommand = new Command("chassisplan", "chassis plan commands");
rootCommand.Add(chassisPlanCommand);

var chassisPlanApplyCommand = new Command("apply", "apply chassis plan");
chassisPlanApplyCommand.AddOption(fileOption);
chassisPlanApplyCommand.SetHandler(ApplyChassisPlan, logLevelOptions, fileOption);
chassisPlanCommand.AddCommand(chassisPlanApplyCommand);

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
var entryAssembly = Assembly.GetEntryAssembly()!;
var fileVersionInfo = FileVersionInfo.GetVersionInfo(entryAssembly.Location);
var productVersion = fileVersionInfo.ProductVersion ?? "unknown";
Console.WriteLine($"OVN Agent {productVersion} (--help for help, Ctrl+C to exit)");
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

static Task RunPrimaryCommand(LogLevel logLevel)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureHostOptions(cfg => cfg.ShutdownTimeout = TimeSpan.FromSeconds(15))
        .UseWindowsService(cfg => cfg.ServiceName = "ovn-primary")
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddSystemEnvironment(services);
            AddRemoteSettings(services, "primary", IPAddress.Parse("192.168.240.101"), Prelude.Map(("extern", "br-extern")));

            services.AddHostedNode<OVNDatabaseNode>();
            services.AddHostedNode<NetworkControllerNode>();
            services.AddHostedNode<OVSDbNode>();
            services.AddHostedNode<OVSSwitchNode>();
            services.AddHostedNode<OVNChassisNode>();
        })
        .Build();

    return host.RunAsync();
}

static Task RunSecondaryCommand(LogLevel logLevel)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureHostOptions(cfg => cfg.ShutdownTimeout = TimeSpan.FromSeconds(15))
        .UseWindowsService(cfg => cfg.ServiceName = "ovn-secondary")
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddSystemEnvironment(services);
            AddRemoteSettings(services, "secondary", IPAddress.Parse("192.168.240.102"));

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

static async Task<int> ApplyClusterPlan(LogLevel logLevel, FileInfo clusterPlanFile)
{
    string yaml = await ReadTextAsync(clusterPlanFile);
    var netplan = ClusterPlanParser.ParseYaml(yaml);

    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {

            AddOVNCore(services);
            services.AddSingleton(
                sp => new ClusterPlanRealizer(
                    new OVNControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().NorthDBConnection),
                    new OVNSouthboundControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().SouthDBConnection),
                    sp.GetRequiredService<ILogger<ClusterPlanRealizer>>()));
        })
        .Build();

    var result = await host.Services.GetRequiredService<ClusterPlanRealizer>()
        .ApplyClusterPlan(netplan);

    return result.Match(
        Right: _ => 0,
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(
                Error.New("Failed to apply cluster plan.", error)));
            return -1;
        });
}

static async Task<int> ApplyChassisPlan(LogLevel logLevel, FileInfo chassisPlanFile)
{
    string yaml = await ReadTextAsync(chassisPlanFile);
    var netplan = ChassisPlanParser.ParseYaml(yaml);

    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {

            AddOVNCore(services);
            services.AddSingleton(
                sp => new ChassisPlanRealizer(
                    sp.GetRequiredService<ISystemEnvironment>(),
                    new OVSControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        LocalConnections.Switch),
                    sp.GetRequiredService<ILogger<ChassisPlanRealizer>>()));
        })
        .Build();

    var result = await host.Services.GetRequiredService<ChassisPlanRealizer>()
        .ApplyChassisPlan(netplan);

    return result.Match(
        Right: _ => 0,
        Left: error =>
        {
            Console.WriteLine(ErrorUtils.PrintError(
                Error.New("Failed to apply chassis plan.", error)));
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
    var result = await portManager.GetConfiguredPortName(adapterId);

    return result.Match(
        Right: portName =>
        {
            portName.IfSome(n => Console.WriteLine(n));
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
    AddSystemEnvironment(services);
    services.AddSingleton<IOvsSettings, LocalOVSWithOVNSettings>();
    services.AddSingleton<IOVNSettings, LocalOVSWithOVNSettings>();
}

static void AddRemoteSettings(
    IServiceCollection services,
    string chassisName,
    IPAddress? ipAddress,
    Map<string, string> bridgeMappings = default)
{
    var settings = new RemoteOvsWithOvnSettings(
        LocalConnections.Southbound,
        //new OvsDbConnection("192.168.241.101", 6642),
        chassisName,
        ipAddress,
        bridgeMappings);

    settings.Logging.File.Level = OvsLogLevel.Debug;

    services.AddSingleton<IOvsSettings>(settings);
    services.AddSingleton<IOVNSettings>(settings);
}

static void AddSystemEnvironment(IServiceCollection services)
{
#if WINDOWS
    services.AddSingleton<ISystemEnvironment, WindowsSystemEnvironment>();
#else
    services.AddSingleton<ISystemEnvironment, SystemEnvironment>();
#endif
    services.AddSingleton<IOvsSettings, LocalOVSWithOVNSettings>();
    services.AddSingleton<IOVNSettings, LocalOVSWithOVNSettings>();
}

static async Task<string> ReadTextAsync(FileInfo fileInfo)
{
    using var reader = fileInfo.OpenText();
    return await reader.ReadToEndAsync();
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
