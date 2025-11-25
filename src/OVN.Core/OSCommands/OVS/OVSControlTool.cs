using System.Text;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSControlTool(
    ISystemEnvironment systemEnvironment,
    OvsDbConnection dbConnection,
    bool noWait = false)
    : OVSControlToolBase(systemEnvironment, dbConnection, OVSCommands.VSwitchControl)
{

    protected override string BuildArguments(string command)
    {
        var sb = new StringBuilder();
        if (noWait)
            sb.Append("--no-wait ");
        
        sb.Append(base.BuildArguments(command));
        return sb.ToString();
    }

    public EitherAsync<Error, Unit> InitDb(
        CancellationToken cancellationToken = default) =>
        from _ in RunCommand($"{(noWait ? "" : "--no -wait ")}init", true, cancellationToken)
        select unit;
}
