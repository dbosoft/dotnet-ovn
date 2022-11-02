using System.Net;
using System.Text;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly ISysEnvironment _sysEnv;

    public OVSControlTool(ISysEnvironment sysEnv, OvsDbConnection dbConnection) : base(sysEnv,
        OVSCommands.VSwitchControl)
    {
        _sysEnv = sysEnv;
        _dbConnection = dbConnection;
    }

    protected override string BuildArguments(string command)
    {
        var baseArguments = base.BuildArguments(command);
        var sb = new StringBuilder();
        sb.Append($"--db=\"{_dbConnection.GetCommandString(_sysEnv.FileSystem, false)}\"");
        sb.Append(' ');
        sb.Append(baseArguments);
        return sb.ToString();
    }

    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand("init", cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, Unit> ConfigureOVN(
        OvsDbConnection sbDBConnection,
        string chassisName,
        IPAddress? encapIp = null,
        string encapType = "geneve",
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append(
            $"-- set open . external-ids:ovn-remote=\"{sbDBConnection.GetCommandString(_sysEnv.FileSystem, false)}\" ");
        sb.Append($"-- set open . external-ids:ovn-encap-type={encapType} ");
        sb.Append($"-- set open . external-ids:ovn-encap-ip={encapIp ?? IPAddress.Loopback} ");
        sb.Append($"-- set open . external-ids:system-id={chassisName} ");

        return RunCommand(sb.ToString(), cancellationToken).Map(_ => Unit.Default);
    }
}