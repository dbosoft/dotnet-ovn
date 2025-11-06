using System.Net;
using System.Text;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVSControlTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection)
        : base(systemEnvironment, OVSCommands.VSwitchControl)
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
        return RunCommand("--no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, Unit> ConfigureOVN(
        OvsDbConnection sbDBConnection,
        string chassisName,
        IPAddress? encapIp = null,
        string encapType = "geneve",
        bool noWait = false,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        if (noWait) sb.Append(" --no-wait ");
        sb.Append(
            $"-- set open . external-ids:ovn-remote=\"{sbDBConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\" ");
        sb.Append($"-- set open . external-ids:ovn-encap-type={encapType} ");
        sb.Append($"-- set open . external-ids:ovn-encap-ip={encapIp ?? IPAddress.Loopback} ");
        sb.Append($"-- set open . external-ids:system-id={chassisName} ");

        return RunCommand(sb.ToString(), true, cancellationToken).Map(_ => Unit.Default);
    }
}