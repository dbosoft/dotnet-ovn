using System.Text;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNSouthboundControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVNSouthboundControlTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection)
        : base(systemEnvironment, OVNCommands.SouthboundControl)
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
}
