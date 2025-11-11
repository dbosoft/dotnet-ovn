using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ClusterPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvnControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyClusterPlan_NewPlan_IsSuccessful()
    {
        var clusterPlan = new ClusterPlan()
            .AddChassisGroup("chassis-group-1")
            .AddChassis("chassis-group-1", "chassis-1", 10)
            .AddChassis("chassis-group-1", "chassis-2", 20);

        await ApplyClusterPlan(clusterPlan);

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyClusterPlan_ChassisGroupChanged_IsSuccessful()
    {
        var clusterPlan = new ClusterPlan()
            .AddChassisGroup("chassis-group-1")
            .AddChassis("chassis-group-1", "chassis-2", 25)
            .AddChassis("chassis-group-1", "chassis-3", 50);

        await ApplyClusterPlan(clusterPlan);

        await VerifyDatabase();
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanNorthboundRealizer(ControlTool, NullLogger.Instance);

        (await realizer.ApplyClusterPlan(clusterPlan)).ThrowIfLeft();
    }
}
