using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class OvsControlToolTests(
    ITestOutputHelper testOutputHelper)
    : OvsControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ConfigureOVN_WithLocalSettings_IsSuccessful()
    {
        var either = await ControlTool.ConfigureOVN(
            LocalConnections.Southbound,
            "local",
            noWait: true);
        either.ThrowIfLeft();

        await VerifyDatabase();
    }
}
