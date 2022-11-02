using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

public class VSwitchDProcess : DemonProcessBase
{
    private readonly VSwitchDSettings _settings;
    private readonly ISysEnvironment _sysEnv;

    public VSwitchDProcess(ISysEnvironment sysEnv, VSwitchDSettings settings, ILogger logger)
        : base(sysEnv,
            OVSCommands.VSwitchDemon,
            new OvsFile("/var/run/openvswitch", "ovs-vswitchd.ctl"), logger)
    {
        _sysEnv = sysEnv;
        _settings = settings;
    }

    protected override string BuildArguments()
    {
        var dbConnection = _settings.DbConnection.GetCommandString(_sysEnv.FileSystem, false);

        var baseArguments = base.BuildArguments();

        var sb = new StringBuilder();
        sb.Append($"\"{dbConnection}\"");
        sb.Append(' ');
        sb.Append(baseArguments);

        return sb.ToString();
    }
}