using System.Text;
using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVNControlTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection)
        : base(systemEnvironment, OVNCommands.NorthboundControl)
    {
        _systemEnvironment = systemEnvironment;
        _dbConnection = dbConnection;
    }

    protected override string BuildArguments(string command)
    {
        var baseArguments = base.BuildArguments(command);
        var sb = new StringBuilder();
        sb.Append($"--db=\"{_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\"");
        sb.Append(' ');
        sb.Append(baseArguments);
        return sb.ToString();
    }
    
    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand(" --no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }
}