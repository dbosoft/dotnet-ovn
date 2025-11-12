using System.Net;
using AwesomeAssertions;
using Dbosoft.OVN.Model.OVN;
using Dbosoft.OVN.OSCommands.OVN;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ClusterPlanSouthboundRealizerTests(ITestOutputHelper testOutputHelper)
    : OvnSouthboundControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyClusterPlan_NewPlan_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        await VerifyDatabase();

        // Verify that the OVN Southbound database is listening on the expected port
        var networkControlTool = new OVNSouthboundControlTool(
            SystemEnvironment,
            new OvsDbConnection("127.0.0.1", 42421));
        var either = await networkControlTool.FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection);

        var records = either.ThrowIfLeft();
        records.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplyClusterPlan_UpdatedPlan_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        var updatedPlan = new ClusterPlan()
            .AddSouthboundConnection(42423);

        await ApplyClusterPlan(updatedPlan);

        await VerifyDatabase();

        // Verify that the OVN Southbound database is listening on the expected port
        var networkControlTool = new OVNSouthboundControlTool(
            SystemEnvironment,
            new OvsDbConnection("127.0.0.1", 42423));
        var either = await networkControlTool.GetRecord<SouthboundGlobal>(
            OVNSouthboundTableNames.Global,
            ".");

        var record = either.ThrowIfLeft();
        record.IsSome.Should().BeTrue();
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanSouthboundRealizer(ControlTool, NullLogger.Instance);

        (await realizer.ApplyClusterPlan(clusterPlan)).ThrowIfLeft();
    }

    private ClusterPlan CreateClusterPlan() =>
        new ClusterPlan()
            .AddSouthboundConnection(42421)
            .AddSouthboundConnection(42422, true, IPAddress.Parse("203.0.113.2"));
}
