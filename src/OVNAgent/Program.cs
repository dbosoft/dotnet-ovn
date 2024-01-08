

using System.CommandLine;
using System.Diagnostics;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVNAgent;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;


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

var installServiceCommand = new Command("install", "install OVN as service");
installServiceCommand.AddOption(nodeTypeOptions);
installServiceCommand.SetHandler((l, n) => ManageServiceCommand(true, l, n), logLevelOptions, nodeTypeOptions );
serviceCommand.AddCommand(installServiceCommand);

var removeServiceCommand = new Command("remove", "removes OVN service");
removeServiceCommand.AddOption(nodeTypeOptions);
removeServiceCommand.SetHandler((l, n) => ManageServiceCommand(false, l, n), logLevelOptions, nodeTypeOptions );

serviceCommand.AddCommand(removeServiceCommand);


return await rootCommand.InvokeAsync(args);

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

            if (nodeType is not (NodeType.AllInOne or NodeType.OVNCentral or NodeType.OVNController)) return;
            
            services.AddHostedNode<NetworkControllerNode>();
            
            
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


static async Task ApplyNetplan(LogLevel logLevel, FileInfo netplanFile)
{
    var serializer = new DeserializerBuilder()
//        .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
                        sp.GetRequiredService<ISysEnvironment>(),
                        sp.GetRequiredService<IOVNSettings>().NorthDBConnection),
                    sp.GetRequiredService<ILogger<NetworkPlanRealizer>>()));
        })
        .Build();

    await host.Services.GetRequiredService<NetworkPlanRealizer>()
        .ApplyNetworkPlan(netplan)
        .IfLeft(l =>
        {
            host.Services.GetRequiredService<ILogger<NetworkPlanRealizer>>().LogError(l.Message);
        });
    
    
}


static async Task ManageServiceCommand(bool install, LogLevel logLevel, NodeType nodeType)
{
    
    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(AddOVNCore)
        .Build();

    var serviceManager = host.Services.GetRequiredService<ISysEnvironment>().GetServiceManager(
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

static void AddOVNCore(IServiceCollection services)
{
    services.AddSingleton<ISysEnvironment, SystemEnvironment>();
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