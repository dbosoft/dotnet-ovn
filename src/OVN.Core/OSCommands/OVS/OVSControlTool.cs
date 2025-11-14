using System.Text;
using LanguageExt;
using LanguageExt.Common;

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

    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand("--no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }
}
