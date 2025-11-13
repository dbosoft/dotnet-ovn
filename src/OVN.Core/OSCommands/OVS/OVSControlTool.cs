using System.Data;
using System.Net;
using System.Text;
using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly bool _noWait;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVSControlTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection,
        bool noWait = false)
        : base(systemEnvironment, OVSCommands.VSwitchControl)
    {
        _systemEnvironment = systemEnvironment;
        _dbConnection = dbConnection;
        _noWait = noWait;
    }

    protected override string BuildArguments(string command)
    {
        var sb = new StringBuilder();
        if (_noWait)
            sb.Append("--no-wait ");
        
        sb.Append($"--db=\"{_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\" ");
        sb.Append(base.BuildArguments(command));
        return sb.ToString();
    }

    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand("--no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }
}
