using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

/// <summary>
/// Northbound test base which starts the northbound database with the
/// remote listener and SSL configuration read from the database (like the
/// southbound database). This allows testing remote and SSL connections to
/// the northbound database.
/// </summary>
public abstract class OvnNorthboundRemoteControlToolTestBase : OvsDbTestBase
{
    private static readonly OVSDbSettings DbSettings = OVSDbSettingsBuilder
        .ForNorthbound()
        .UseRemoteConfigsFromDatabase(true)
        .Build();

    protected readonly OVNControlTool ControlTool;

    protected OvnNorthboundRemoteControlToolTestBase(
        ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, DbSettings)
    {
        ControlTool = new OVNControlTool(SystemEnvironment, DbSettings.Connection);
    }

    protected override EitherAsync<Error, Unit> InitializeDatabase() =>
        ControlTool.InitDb();

    protected async Task VerifyDatabase()
    {
        await VerifyDatabase("OVN_Northbound", "7.18.0");
    }
}
