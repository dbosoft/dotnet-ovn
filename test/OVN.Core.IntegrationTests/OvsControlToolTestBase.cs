using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public abstract class OvsControlToolTestBase : OvsDbTestBase
{
    private static readonly OVSDbSettings DbSettings = OVSDbSettingsBuilder.ForSwitch().Build();

    protected readonly OVSControlTool ControlTool;

    protected OvsControlToolTestBase(
        ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, DbSettings)
    {
        ControlTool = new OVSControlTool(SystemEnvironment, DbSettings.Connection);
    }

    protected override EitherAsync<Error, Unit> InitializeDatabase() =>
        ControlTool.InitDb();

    protected async Task VerifyDatabase()
    {
        await VerifyDatabase("Open_vSwitch");
    }
}
