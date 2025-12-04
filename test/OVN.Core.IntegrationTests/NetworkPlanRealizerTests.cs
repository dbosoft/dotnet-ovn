using System.Net;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class NetworkPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvnControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyNetworkPlan_NewPlan_PlanIsApplied()
    {
        var networkPlan = CreateNetworkPlan();

        await ApplyNetworkPlan(networkPlan);

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyNetworkPlan_UpdatedPlan_PlanIsApplied()
    {
        await ApplyNetworkPlan(CreateNetworkPlan());

        var updatedPlan = new NetworkPlan("test-project")
            .AddRouter("router-2")
            .AddRouterPort(
                "provider-switch-2",
                "router-2",
                "02:00:00:00:00:01",
                IPAddress.Parse("192.0.2.100"),
                IPNetwork2.Parse("192.0.2.0/24"),
                chassisGroup: "local")
            .AddRouterPort(
                "switch-2",
                "router-2",
                "02:00:00:00:00:02",
                IPAddress.Parse("198.51.100.1"),
                IPNetwork2.Parse("198.51.100.0/24"))
            .AddSourceNATRule(
                "router-2",
                IPAddress.Parse("192.0.2.100"),
                IPNetwork2.Parse("198.51.100.0/24"))
            .AddDestinationNATRule(
                "router-2",
                IPAddress.Parse("192.0.2.110"),
                "02:00:00:00:00:03",
                IPAddress.Parse("198.51.100.100"))
            .AddStaticRoute(
                "router-2",
                "0.0.0.0/0",
                IPAddress.Parse("198.0.2.1"))
            .AddSwitch("provider-switch-2")
            .AddExternalNetworkPort("provider-switch-2", "extern", 5)
            .AddSwitch("switch-2")
            .AddNetworkPort(
                "switch-2",
                "switch2-port-1",
                "02:01:00:00:00:01",
                IPAddress.Parse("198.51.100.100"),
                dhcpOptionsV4: "dhcp-2")
            .AddDHCPOptions(
                "dhcp-2",
                IPNetwork2.Parse("198.51.100.0/24"),
                Empty)
            .AddDnsRecords(
                "dns-2",
                Map(("vm-1.acme.test", "198.51.100.0")),
                Empty);

        await ApplyNetworkPlan(updatedPlan);

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyNetworkPlan_RemoveChildResources_PlanIsApplied()
    {
        await ApplyNetworkPlan(CreateNetworkPlan());

        var updatedPlan = new NetworkPlan("test-project")
            .AddRouter("router-1")
            .AddSwitch("provider-switch-1")
            .AddSwitch("switch-1");

        await ApplyNetworkPlan(updatedPlan);

        await VerifyDatabase();
    }

    private async Task ApplyNetworkPlan(NetworkPlan networkPlan)
    {
        var realizer = new NetworkPlanRealizer(SystemEnvironment, ControlTool, NullLogger.Instance);

        (await realizer.ApplyNetworkPlan(networkPlan)).ThrowIfLeft();
    }

    private NetworkPlan CreateNetworkPlan() =>
        new NetworkPlan("test-project")
            .AddRouter("router-1")
            .AddRouterPort(
                "provider-switch-1",
                "router-1",
                "02:00:00:00:00:01",
                IPAddress.Parse("192.0.2.100"),
                IPNetwork2.Parse("192.0.2.0/24"),
                chassisGroup: "local")
            .AddRouterPort(
                "switch-1",
                "router-1",
                "02:00:00:00:00:02",
                IPAddress.Parse("198.51.100.1"),
                IPNetwork2.Parse("198.51.100.0/24"))
            .AddSourceNATRule(
                "router-1",
                IPAddress.Parse("192.0.2.100"),
                IPNetwork2.Parse("198.51.100.0/24"))
            .AddDestinationNATRule(
                "router-1",
                IPAddress.Parse("192.0.2.110"),
                "02:00:00:00:00:03",
                IPAddress.Parse("198.51.100.100"))
            .AddStaticRoute(
                "router-1",
                "0.0.0.0/0",
                IPAddress.Parse("198.0.2.1"))
            .AddSwitch("provider-switch-1")
            .AddExternalNetworkPort("provider-switch-1", "extern", 5)
            .AddSwitch("switch-1")
            .AddNetworkPort(
                "switch-1",
                "switch-1-port-1",
                "02:01:00:00:00:01",
                IPAddress.Parse("198.51.100.100"),
                dhcpOptionsV4: "dhcp-1")
            .AddDHCPOptions(
                "dhcp-1",
                IPNetwork2.Parse("198.51.100.0/24"),
                Empty)
            .AddDnsRecords(
                "dns-1",
                Map(("vm-1.acme.test", "198.51.100.0")),
                Empty);
}
