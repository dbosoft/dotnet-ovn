using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ProjectPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvsDbTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyNetworkPlan_NewPlan_IsSuccessful()
    {
        var networkPlan = new NetworkPlan("test-project")
            .AddSwitch("switch-1")
            .AddNetworkPort(
                "switch-1",
                "switch-1-port-1",
                "02:00:00:00:00:01",
                IPAddress.Parse("192.0.2.1"))
            .AddNetworkPort(
                "switch-1",
                "switch-1-port-2",
                "02:00:00:00:00:02",
                IPAddress.Parse("192.0.2.2"));

        var controlTool = new OVNControlTool(SystemEnvironment, DbConnection);
        (await controlTool.InitDb()).ThrowIfLeft();

        var ovsDbClientTool = new OVSDbClientTool(SystemEnvironment, DbConnection);
        var databases = (await ovsDbClientTool.ListDatabases()).ThrowIfLeft();

        var s = DbConnection.GetCommandString(SystemEnvironment.FileSystem, false);

        var realizer = new NetworkPlanRealizer(controlTool, NullLogger.Instance);

        (await realizer.ApplyNetworkPlan(networkPlan)).ThrowIfLeft();

        var content = await DumpDatabase("OVN_Northbound");
        await VerifyJson(content);
    }

    [Fact]
    public async Task ApplyNetworkPlan_UpdatedPlan_IsSuccessful()
    {
        var initialPlan = new NetworkPlan("test-project")
            .AddSwitch("switch-1")
            .AddNetworkPort(
                "switch-1",
                "switch-1-port-1",
                "02:00:00:00:00:01",
                IPAddress.Parse("192.0.2.1"))
            .AddNetworkPort(
                "switch-1",
                "switch-1-port-2",
                "02:00:00:00:00:02",
                IPAddress.Parse("192.0.2.2"));

        await ApplyNetworkPlan(initialPlan);

        var updatedPlan = new NetworkPlan("test-project")
            .AddSwitch("switch-2")
            .AddNetworkPort(
                "switch-2",
                "switch-2-port-1",
                "02:00:00:00:00:01",
                IPAddress.Parse("192.0.2.1"))
            .AddNetworkPort(
                "switch-2",
                "switch-2-port-2",
                "02:00:00:00:00:02",
                IPAddress.Parse("192.0.2.2"));

        await ApplyNetworkPlan(updatedPlan);

        await VerifyDatabase();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var controlTool = new OVNControlTool(SystemEnvironment, DbConnection);
        (await controlTool.InitDb()).ThrowIfLeft();
    }

    private async Task ApplyNetworkPlan(NetworkPlan networkPlan)
    {
        var controlTool = new OVNControlTool(SystemEnvironment, DbConnection);
        var realizer = new NetworkPlanRealizer(controlTool, NullLogger.Instance);

        (await realizer.ApplyNetworkPlan(networkPlan)).ThrowIfLeft();
    }

    private async Task VerifyDatabase()
    {
        await VerifyDatabase("OVN_Northbound");
    }
}
