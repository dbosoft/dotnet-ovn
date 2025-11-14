using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public abstract class OvnControlToolTestBase : OvsDbTestBase
{
    private static readonly OVSDbSettings DbSettings = OVSDbSettingsBuilder.ForNorthbound().Build();

    protected readonly OVNControlTool ControlTool;

    protected OvnControlToolTestBase(
        ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, DbSettings)
    {
        ControlTool = new OVNControlTool(SystemEnvironment, DbSettings.Connection);
    }

    protected override EitherAsync<Error, Unit> InitializeDatabase() =>
        ControlTool.InitDb();

    protected async Task VerifyDatabase()
    {
        await VerifyDatabase("OVN_Northbound", "7.11.0");
    }
}
