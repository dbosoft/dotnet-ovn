using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ChassisPlanRealizerTests(
    ITestOutputHelper testOutputHelper)
    : OvsControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyChassisPlan_NewPlan_IsSuccessful()
    {
        var chassisPlan = new ChassisPlan("chassis-1")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.1"))
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.2"))
            .AddBridgeMapping("extern", "br-extern");

        await ApplyChassisPlan(chassisPlan);

        await VerifyDatabase();
    }

    private async Task ApplyChassisPlan(ChassisPlan chassisPlan)
    {
        var realizer = new ChassisPlanRealizer(SystemEnvironment, ControlTool, NullLogger.Instance);

        (await realizer.ApplyChassisPlan(chassisPlan)).ThrowIfLeft();
    }
}
