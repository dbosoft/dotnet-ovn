using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNControllerProcess : DemonProcessBase
{
    private readonly OVNControllerSettings _settings;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVNControllerProcess(
        ISystemEnvironment systemEnvironment,
        OVNControllerSettings settings,
        ILogger logger)
        : base(systemEnvironment,
            OVNCommands.OVNController,
            new OvsFile("/var/run/ovn", "ovn-controller.ctl"),
            new OvsFile("/var/log/ovn", "ovn-controller.log"),
            settings.LoggingSettings,
            true,
            settings.AllowAttach,
            logger)
    {
        _systemEnvironment = systemEnvironment;
        _settings = settings;
        NoControlFileArgument = true;
    }

    protected override string BuildArguments()
    {
        var ovsDbConnection = _settings.OvsDbConnection.GetCommandString(_systemEnvironment.FileSystem, false);
        var baseArguments = base.BuildArguments();

        var sb = new StringBuilder();
        sb.Append(baseArguments);
        sb.Append(' ');
        sb.Append($"\"{ovsDbConnection}\"");
        return sb.ToString();
    }
}