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
            new OvsDbConnection("203.0.113.1", 6641),
            "local");
        either.ThrowIfLeft();

        await VerifyDatabase();
    }
}
