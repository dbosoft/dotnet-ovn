using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class OvnSouthboundControlToolTestBase : OvsDbTestBase
{
    private static readonly OVSDbSettings DbSettings = OVSDbSettingsBuilder
        .ForSouthbound()
        .UseRemoteConfigsFromDatabase(true)
        .Build();

    protected readonly OVNSouthboundControlTool ControlTool;

    protected OvnSouthboundControlToolTestBase(
        ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, DbSettings)
    {
        ControlTool = new OVNSouthboundControlTool(SystemEnvironment, DbSettings.Connection);
    }

    protected override EitherAsync<Error, Unit> InitializeDatabase() => unit;

    protected async Task VerifyDatabase()
    {
        await VerifyDatabase("OVN_Southbound");
    }
}
