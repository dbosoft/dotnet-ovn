using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVS;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVNDatabaseNode : DemonNodeBase
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly IOVNSettings _ovnSettings;

    // runs: OVNSouthboundDB
    // connects to: other OVNDatabaseNodes (DB cluster)
    private readonly ISystemEnvironment _systemEnvironment;

    public OVNDatabaseNode(ISystemEnvironment systemEnvironment,
        IOVNSettings ovnSettings,
        ILoggerFactory loggerFactory)
    {
        _systemEnvironment = systemEnvironment;
        _ovnSettings = ovnSettings;
        _loggerFactory = loggerFactory;
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        yield return new OVSDBProcess(_systemEnvironment,
            new OVSDbSettings(
                _ovnSettings.SouthDBConnection,
                new OvsFile("etc/ovn", "ovn_sb.db"),
                // ReSharper disable StringLiteralTypo
                new OvsFile("usr/share/ovn", "ovn-sb.ovsschema"),
                // ReSharper restore StringLiteralTypo
                new OvsFile("var/run/ovn", "ovn-sb.ctl"),
                new OvsFile("var/log/ovn", "ovn-sb.log"),
                _ovnSettings.Logging,
                false),
            _loggerFactory.CreateLogger<OVSDBProcess>());
    }
}