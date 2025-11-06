using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class NetworkPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvnControlToolTestBase(testOutputHelper)
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

        await ApplyNetworkPlan(networkPlan);

        await VerifyDatabase();
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

    private async Task ApplyNetworkPlan(NetworkPlan networkPlan)
    {
        var realizer = new NetworkPlanRealizer(ControlTool, NullLogger.Instance);

        (await realizer.ApplyNetworkPlan(networkPlan)).ThrowIfLeft();
    }
}
