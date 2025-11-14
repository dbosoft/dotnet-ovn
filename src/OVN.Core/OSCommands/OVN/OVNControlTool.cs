using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNControlTool(
    ISystemEnvironment systemEnvironment,
    OvsDbConnection dbConnection)
    : OVSControlToolBase(systemEnvironment, dbConnection, OVNCommands.NorthboundControl)
{
    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand(" --no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }
}
