using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using Dbosoft.OVN.SimplePki;
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

if (!AdminGuard.IsElevated())
{
    await Console.Error.WriteLineAsync(
        "This command requires elevated privileges. Please run the command as an administrator.");
    // Return the proper HResult / errno for permission denied
    return OperatingSystem.IsWindows() ? unchecked((int)0x80070005) : 0x0d;
}

var logLevelOptions = new System.CommandLine.Option<LogLevel>("--logLevel", () => LogLevel.Information);
var nodeTypeOptions = new System.CommandLine.Option<NodeType>("--nodes", () => NodeType.AllInOne );
var fileOption = new System.CommandLine.Option<FileInfo>("--file");

var rootCommand = new RootCommand();
rootCommand.AddGlobalOption(logLevelOptions);

var runCommand = new Command("run", "Runs OVN and OVS locally.");
runCommand.AddOption(nodeTypeOptions);
runCommand.SetHandler(RunCommand, logLevelOptions, nodeTypeOptions);
rootCommand.Add(runCommand);

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

var pkiCommand = new Command("pki", "PKI commands");
rootCommand.Add(pkiCommand);

var pkiInitCommand = new Command("init", "Initialize a new PKI");
pkiInitCommand.SetHandler(InitializePki, logLevelOptions);
pkiCommand.AddCommand(pkiInitCommand);

var pkiGenerateChassisCommand = new Command("generate-chassis", "Generates a SSL certificate for a chassis");
var chassisNameArgument = new Argument<string>("chassisName", "OVN chassis name");
pkiGenerateChassisCommand.AddArgument(chassisNameArgument);
pkiGenerateChassisCommand.SetHandler(CreateChassisPki, logLevelOptions, chassisNameArgument);
pkiCommand.AddCommand(pkiGenerateChassisCommand);


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
            
            if (nodeType is NodeType.AllInOne or NodeType.OVNCentral)
            {
                services.AddHostedNode<OVNDatabaseNode>();
                services.AddHostedNode<NetworkControllerNode>();
            }

            if (nodeType is NodeType.AllInOne or NodeType.Chassis)
            {
                services.AddHostedNode<OVSDbNode>();
                services.AddHostedNode<OVSSwitchNode>();
                services.AddHostedNode<OVNChassisNode>();
            }
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
                    sp.GetRequiredService<ISystemEnvironment>(),
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
                    sp.GetRequiredService<ISystemEnvironment>(),
                    new OVNControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().NorthDBConnection),
                    new OVNSouthboundControlTool(
                        sp.GetRequiredService<ISystemEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().SouthDBConnection)));
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
                        LocalConnections.Switch)));
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

static async Task<int> InitializePki(LogLevel logLevel)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddOVNCore(services);
            services.AddSingleton<IPkiService, PkiService>();
        })
        .Build();

    await host.Services.GetRequiredService<IPkiService>().InitializeAsync();
    return 0;
}

static async Task<int> CreateChassisPki(LogLevel logLevel, string chassisName)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddOVNCore(services);
            services.AddSingleton<IPkiService, PkiService>();
        })
        .Build();

    var pkiResult = await host.Services.GetRequiredService<IPkiService>()
        .GenerateChassisPkiAsync(chassisName);

    var config = new ChassisPkiOutput()
    {
        PrivateKey = pkiResult.PrivateKey,
        Certificate = pkiResult.Certificate,
        CaCertificate = pkiResult.CaCertificate
    };

    var yaml = PlanYamlSerializer.Serialize(config);
    Console.WriteLine(yaml);

    return 0;
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
/// The type of node which is started.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Runs all OVN and OVS processes.
    /// </summary>
    AllInOne,
    
    /// <summary>
    /// Runs only the processes which are necessary for
    /// an OVN chassis (ovs processes and ovn-controller).
    /// </summary>
    Chassis,
    
    /// <summary>
    /// Run only the central management processes for OVN
    /// (databases and northd).
    /// </summary>
    OVNCentral,
}
