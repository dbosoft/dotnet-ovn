using System.Text;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public class NorthDProcess : DemonProcessBase
{
    private readonly NorthDSettings _settings;
    private readonly ISysEnvironment _sysEnv;

    public NorthDProcess(ISysEnvironment sysEnv, NorthDSettings settings, ILogger logger)
        : base(sysEnv,
            OVNCommands.NorthboundDemon,
            new OvsFile("/var/run/ovn", "ovn-northd.ctl"), logger)
    {
        _sysEnv = sysEnv;
        _settings = settings;
    }

    protected override string BuildArguments()
    {
        var northDbConnection = _settings.NorthDbConnection.GetCommandString(_sysEnv.FileSystem, false);
        var southDbConnection = _settings.SouthDBConnection.GetCommandString(_sysEnv.FileSystem, false);

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