using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public class NorthDProcess : DemonProcessBase
{
    private readonly NorthDSettings _settings;
    private readonly ISystemEnvironment _systemEnvironment;

    public NorthDProcess(
        ISystemEnvironment systemEnvironment,
        NorthDSettings settings,
        ILogger logger)
        : base(systemEnvironment,
            OVNCommands.NorthboundDemon,
            new OvsFile("/var/run/ovn", "ovn-northd.ctl"),
            new OvsFile("/var/log/ovn", "ovn-northd.log"),
            settings.LoggingSettings,
            true,
            settings.AllowAttach,
            logger)
    {
        _systemEnvironment = systemEnvironment;
        _settings = settings;
    }

    protected override string BuildArguments()
    {
        var northDbConnection = _settings.NorthDbConnection.GetCommandString(_systemEnvironment.FileSystem, false);
        var southDbConnection = _settings.SouthDBConnection.GetCommandString(_systemEnvironment.FileSystem, false);

        var baseArguments = base.BuildArguments();

        var sb = new StringBuilder();
        // ReSharper disable StringLiteralTypo
        sb.Append($"--ovnnb-db=\"{northDbConnection}\"");
        sb.Append(' ');
        sb.Append($"--ovnsb-db=\"{southDbConnection}\"");
        sb.Append(' ');
        sb.Append(baseArguments);
        // ReSharper restore StringLiteralTypo
        return sb.ToString();
    }
}