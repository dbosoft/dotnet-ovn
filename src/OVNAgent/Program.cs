

using System.CommandLine;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVNAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;


var logLevelOptions = new Option<LogLevel>("--logLevel", () => LogLevel.Information);

var rootCommand = new RootCommand();
rootCommand.AddGlobalOption(logLevelOptions);

var runCommand = new Command("run", "Runs OVN and OVS locally.");
runCommand.SetHandler(RunCommand, logLevelOptions);
rootCommand.Add(runCommand);
    
var removeCommand = new Command("remove", "Removes OVS locally.");
removeCommand.SetHandler(RemoveCommand, logLevelOptions);
rootCommand.Add(removeCommand);

var netplanCommand = new Command("netplan", "netplan commands");
rootCommand.Add(netplanCommand);

var applyCommand = new Command("apply", "apply network plan");
var fileOption = new Option<FileInfo>("--file");
applyCommand.AddOption(fileOption);
applyCommand.SetHandler(ApplyNetplan, logLevelOptions, fileOption);
netplanCommand.AddCommand(applyCommand);


return await rootCommand.InvokeAsync(args);


static Task RunCommand(LogLevel logLevel)
{

    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddOVNCore(services);
            services.AddSingleton<OVNDatabaseNode>();
            services.AddSingleton<NetworkControllerNode>();
            services.AddSingleton<OVSChassisNode>();
            services.AddSingleton<OVNChassisNode>();

            services.AddHostedService<OVSNodeService<OVSChassisNode>>();
            services.AddHostedService<OVSNodeService<OVNDatabaseNode>>();
            services.AddHostedService<OVSNodeService<NetworkControllerNode>>();
            services.AddHostedService<OVSNodeService<OVNChassisNode>>();
        })
        .Build();

    return host.RunAsync();
}

static async Task RemoveCommand(LogLevel logLevel)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureLogging(cfg => cfg.SetMinimumLevel(logLevel))
        .ConfigureServices(services =>
        {
            AddOVNCore(services);
            services.AddSingleton<OVSChassisNode>();
        })
        .Build();

    await host.Services.GetRequiredService<OVSChassisNode>()
        .Remove()
        .IfLeft(l =>
        {
            host.Services.GetRequiredService<ILogger<OVSChassisNode>>().LogError(l.Message);
        });
    
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

static void AddOVNCore(IServiceCollection services)
{
    services.AddSingleton<ISysEnvironment, SystemEnvironment>();
    services.AddSingleton<IOVNSettings, LocalOVSWithOVNSettings>();
   
}
