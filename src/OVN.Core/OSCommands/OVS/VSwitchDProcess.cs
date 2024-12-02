using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

public class VSwitchDProcess : DemonProcessBase
{
    private readonly VSwitchDSettings _settings;
    private readonly ISystemEnvironment _systemEnvironment;

    public VSwitchDProcess(
        ISystemEnvironment systemEnvironment,
        VSwitchDSettings settings,
        bool fallback,
        ILogger logger)
        : base(systemEnvironment,
            OVSCommands.VSwitchDemon,
            new OvsFile("/var/run/openvswitch", fallback ? "ovs-vswitchd2.ctl" : "ovs-vswitchd.ctl"),
            new OvsFile("/var/log/openvswitch", fallback ? "ovs-vswitchd2.log" : "ovs-vswitchd.log"),
            settings.LoggingSettings,
            false,
            settings.AllowAttach,
            logger)
    {
        _systemEnvironment = systemEnvironment;
        _settings = settings;
    }

    protected override string BuildArguments()
    {
        var dbConnection = _settings.DbConnection.GetCommandString(_systemEnvironment.FileSystem, false);

        var baseArguments = base.BuildArguments();

        var sb = new StringBuilder();
        sb.Append($"\"{dbConnection}\"");
        sb.Append(' ');
        sb.Append(baseArguments);

        return sb.ToString();
    }
}