using AwesomeAssertions;
using LanguageExt.Common;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ChassisPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvsControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyChassisPlan_NewPlan_IsSuccessful()
    {
        await ApplyChassisPlan(CreateChassisPlan());

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyChassisPlan_UpdatedPlan_IsSuccessful()
    {
        await ApplyChassisPlan(CreateChassisPlan());

        var updatedPlan = new ChassisPlan("chassis-1")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.100"))
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.101"))
            .AddBridgeMapping("extern", "br-outside")
            .AddBridgeMapping("other-net", "br-other-net");

        await ApplyChassisPlan(updatedPlan);

        await VerifyDatabase();
    }

    [Fact]
    public async Task ApplyChassisPlan_ChassisIdIsChanged_Fails()
    {
        await ApplyChassisPlan(CreateChassisPlan());

        var updatedPlan = new ChassisPlan("chassis-2")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.1"))
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.2"))
            .AddBridgeMapping("extern", "br-extern");

        var act = () => ApplyChassisPlan(updatedPlan);
        await act.Should().ThrowAsync<ErrorException>()
            .WithMessage("*A different chassis ID ('chassis-1') is already configured*");
    }

    private async Task ApplyChassisPlan(ChassisPlan chassisPlan)
    {
        var realizer = new ChassisPlanRealizer(SystemEnvironment, ControlTool, NullLogger.Instance);

        (await realizer.ApplyChassisPlan(chassisPlan)).ThrowIfLeft();
    }

    private ChassisPlan CreateChassisPlan() =>
        new ChassisPlan("chassis-1")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.1"))
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.2"))
            .AddBridgeMapping("extern", "br-extern");
}
