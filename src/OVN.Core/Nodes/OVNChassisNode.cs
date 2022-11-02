using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVNChassisNode : OVNNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IOVNSettings _ovnSettings;
    // runs: OVSDB, VSwitchD, OVNController
    // connects to OVNDatabaseNode

    private readonly ISysEnvironment _sysEnv;
    private OVSDBProcess? _ovsdbProcess;

    public OVNChassisNode(ISysEnvironment sysEnv,
        IOVNSettings ovnSettings,
        ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _ovnSettings = ovnSettings;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<OVNChassisNode>();
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        yield return new OVNControllerProcess(_sysEnv,
            new OVNControllerSettings(LocalOVSConnection),
            _loggerFactory.CreateLogger<OVNControllerProcess>());
    }


    protected override EitherAsync<Error, Unit> OnProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
        if (process != _ovsdbProcess)
            return Unit.Default;
        return WaitForDbSocket(cancellationToken)
            .Bind(_ => ConfigureController(cancellationToken));
    }

    private EitherAsync<Error, Unit> ConfigureController(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(10000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovsControl = new OVSControlTool(_sysEnv, LocalOVSConnection);
        return ovsControl.ConfigureOVN(_ovnSettings.SouthDBConnection, "local",
            cancellationToken: cts.Token);
    }

    private EitherAsync<Error, Unit> InitDB(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(10000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovsControl = new OVSControlTool(_sysEnv, LocalOVSConnection);
        return ovsControl.InitDb(cts.Token);
    }

    private EitherAsync<Error, Unit> WaitForDbSocket(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(5000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        async Task<Either<Error, Unit>> WaitForDbSocketAsync()
        {
            _logger.LogTrace("OVN chassis node - waiting for ovs database to be started.");
            if (_ovsdbProcess == null)
                return Error.New("OVN chassis node not started");

            return await _ovsdbProcess.WaitForDbSocket(cts.Token).MapAsync(r =>
            {
                if (!r)
                    _logger.LogWarning("OVN chassis node - failed to wait for ovs connection before timeout");
                else
                    _logger.LogTrace("OVN chassis node - ovs database has been started.");

                return Unit.Default;
            });
        }

        return WaitForDbSocketAsync().ToAsync();
    }
}