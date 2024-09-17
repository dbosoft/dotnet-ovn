using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVSDbNode : DemonNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    // runs: OVSDB, VSwitchD, OVNController
    // connects to OVNDatabaseNode

    private readonly ISysEnvironment _sysEnv;
    private readonly IOvsSettings _ovsSettings;
    private OVSDBProcess? _ovsdbProcess;

    public OVSDbNode(
        ISysEnvironment sysEnv,
        IOvsSettings ovsSettings,
        ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _ovsSettings = ovsSettings;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<OVSDbNode>();
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        _ovsdbProcess = new OVSDBProcess(_sysEnv,
            new OVSDbSettings(
                LocalOVSConnection,
                new OvsFile("etc/openvswitch", "ovs.db"),
                // ReSharper disable StringLiteralTypo
                new OvsFile("usr/share/openvswitch", "vswitch.ovsschema"),
                // ReSharper restore StringLiteralTypo
                new OvsFile("var/run/openvswitch", "ovs-db.ctl"),
                new OvsFile("var/log/openvswitch", "ovs-db.log"),
                _ovsSettings.LogFileLevel,
                true),
            _loggerFactory.CreateLogger<OVSDBProcess>());

        yield return _ovsdbProcess;
        
    }


    protected override EitherAsync<Error, Unit> OnProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
            return WaitForDbSocket(cancellationToken)
                .Bind(_ => InitDB(cancellationToken));
    }
    
    private EitherAsync<Error, Unit> WaitForDbSocket(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(new TimeSpan(0,1,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        async Task<Either<Error, Unit>> WaitForDbSocketAsync()
        {
            _logger.LogTrace("OVS db node - waiting for ovs database to be started.");

            return await LocalOVSConnection.WaitForDbSocket(_sysEnv,cts.Token).MapAsync(r =>
            {
                if (!r)
                    _logger.LogWarning("OVS db node - failed to wait for ovs connection before timeout");
                else
                    _logger.LogInformation("OVS db node - ovs database has been started.");

                return Unit.Default;
            });
        }

        return WaitForDbSocketAsync().ToAsync();
    }
    
    private EitherAsync<Error, Unit> InitDB(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(new TimeSpan(0,1,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovsControl = new OVSControlTool(_sysEnv, LocalOVSConnection);
        return ovsControl.InitDb(cts.Token);
    }
}