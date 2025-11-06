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
        yield return new OVSDBProcess(
            _systemEnvironment,
            OVSDbSettingsBuilder.ForSouthbound()
                .WithDbConnection(_ovnSettings.SouthDBConnection)
                .WithLogging(_ovnSettings.Logging)
                .AllowAttach(false)
                .Build(),
            _loggerFactory.CreateLogger<OVSDBProcess>());
    }
}