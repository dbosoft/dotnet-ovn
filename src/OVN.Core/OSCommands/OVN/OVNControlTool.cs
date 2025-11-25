using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNControlTool(
    ISystemEnvironment systemEnvironment,
    OvsDbConnection dbConnection)
    : OVSControlToolBase(systemEnvironment, dbConnection, OVNCommands.NorthboundControl)
{
    public EitherAsync<Error, Unit> InitDb(
        CancellationToken cancellationToken = default) =>
        from _ in RunCommand(" --no-wait init", true, cancellationToken)
        select unit;
}
