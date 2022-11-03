using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNControllerProcess : DemonProcessBase
{
    private readonly OVNControllerSettings _settings;
    private readonly ISysEnvironment _sysEnv;

    public OVNControllerProcess(ISysEnvironment sysEnv, OVNControllerSettings settings, ILogger logger)
        : base(sysEnv,
            OVNCommands.OVNController,
            new OvsFile("/var/run/ovn", "ovn-controller.ctl"), logger)
    {
        _sysEnv = sysEnv;
        _settings = settings;
        NoControlFileArgument = true;
    }

    protected override string BuildArguments()
    {
        var ovsDbConnection = _settings.OvsDbConnection.GetCommandString(_sysEnv.FileSystem, false);
        var baseArguments = base.BuildArguments();

        var sb = new StringBuilder();
        sb.Append(baseArguments);
        sb.Append(' ');
        sb.Append($"\"{ovsDbConnection}\"");
        return sb.ToString();
    }
}