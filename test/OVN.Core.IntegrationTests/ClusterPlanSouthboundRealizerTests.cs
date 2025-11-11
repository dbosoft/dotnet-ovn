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
        const int port = 42421;
        var clusterPlan = new ClusterPlan();
        clusterPlan = clusterPlan with
        {
            PlannedSouthboundConnections = clusterPlan.PlannedSouthboundConnections
                .Add("ptcp:42421", new PlannedSouthboundConnection { Target = "ptcp:42421", })
                .Add("tcp:203.0.113.1:16642", new PlannedSouthboundConnection { Target = "tcp:203.0.113.1:16642", }),
        };

        await ApplyClusterPlan(clusterPlan);

        await VerifyDatabase();

        // Verify that the OVN Southbound database is listening on the expected port
        var networkControlTool = new OVNSouthboundControlTool(
            SystemEnvironment,
            new OvsDbConnection("127.0.0.1", port));
        var either = await networkControlTool.FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection);
        
        var records = either.ThrowIfLeft();
        records.Should().HaveCount(2);
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanSouthboundRealizer(ControlTool, NullLogger.Instance);

        (await realizer.ApplyClusterPlan(clusterPlan)).ThrowIfLeft();
    }
}
