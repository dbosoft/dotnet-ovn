using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ClusterPlanNorthboundRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvnControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyClusterPlan_NewPlan_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyClusterPlan_ChassisGroupChanged_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        var updatedPlan = new ClusterPlan()
            .AddChassisGroup("chassis-group-2")
            .AddChassis("chassis-group-2", "chassis-3", 25)
            .AddChassis("chassis-group-2", "chassis-4", 50);

        await ApplyClusterPlan(updatedPlan);

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyClusterPlan_RemoveChassisFromGroup_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        var updatedPlan = new ClusterPlan()
            .AddChassisGroup("chassis-group-1")
            .AddChassis("chassis-group-1", "chassis-1", 10);

        await ApplyClusterPlan(updatedPlan);

        await VerifyDatabase();
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanNorthboundRealizer(ControlTool, NullLogger.Instance);

        (await realizer.ApplyClusterPlan(clusterPlan)).ThrowIfLeft();
    }

    private ClusterPlan CreateClusterPlan() =>
        new ClusterPlan()
            .AddChassisGroup("chassis-group-1")
            .AddChassis("chassis-group-1", "chassis-1", 10)
            .AddChassis("chassis-group-1", "chassis-2", 20);
}
