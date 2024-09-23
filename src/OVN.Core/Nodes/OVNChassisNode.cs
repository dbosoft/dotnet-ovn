using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVNChassisNode : DemonNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IOVNSettings _ovnSettings;
    // runs: OVSDB, VSwitchD, OVNController
    // connects to OVNDatabaseNode

    private readonly ISysEnvironment _sysEnv;

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
            new OVNControllerSettings(LocalOVSConnection, _ovnSettings.Logging, false),
            _loggerFactory.CreateLogger<OVNControllerProcess>());
    }


    protected override EitherAsync<Error, Unit> OnProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
        return WaitForDbSocket(cancellationToken)
            .Bind(_ => ConfigureController(cancellationToken));

    }

    private EitherAsync<Error, Unit> ConfigureController(CancellationToken cancellationToken)
    {
        // when init is still running this could take a while...
        var timeout = new CancellationTokenSource(new TimeSpan(0,5,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovsControl = new OVSControlTool(_sysEnv, LocalOVSConnection);
        return ovsControl.ConfigureOVN(_ovnSettings.SouthDBConnection, "local",
            cancellationToken: cts.Token);
    }
    
    private EitherAsync<Error, Unit> WaitForDbSocket(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(new TimeSpan(0,1,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        async Task<Either<Error, Unit>> WaitForDbSocketAsync()
        {
            _logger.LogTrace("OVN chassis node - waiting for ovs database to be started.");

            return await LocalOVSConnection.WaitForDbSocket(_sysEnv,cts.Token).MapAsync(r =>
            {
                if (!r)
                    _logger.LogWarning("OVN chassis node - failed to wait for ovs connection before timeout");
                else
                    _logger.LogInformation("OVN chassis node - ovs database has been started.");

                return Unit.Default;
            });
        }

        return WaitForDbSocketAsync().ToAsync();
    }
    
}