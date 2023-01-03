using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class NetworkControllerNode : DemonNodeBase
{
    private readonly OvsFile _ctlFile = new("var/run/ovn", "ovn_nb.ctl");
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IOVNSettings _ovnSettings;
    // runs: OVNNorthboundDB, NorthD
    // connects to: other NetworkControllers (DB Cluster), OVNDatabaseNode

    private readonly ISysEnvironment _sysEnv;
    private OVSDBProcess? _northDBProcess;

    public NetworkControllerNode(ISysEnvironment sysEnv,
        IOVNSettings ovnSettings, ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _ovnSettings = ovnSettings;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<NetworkControllerNode>();
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        yield return _northDBProcess = new OVSDBProcess(_sysEnv,
            new OVSDbSettings(
                _ovnSettings.NorthDBConnection,
                new OvsFile("etc/ovn", "ovn_nb.db"),
                // ReSharper disable StringLiteralTypo
                new OvsFile("usr/share/ovn", "ovn-nb.ovsschema"),
                // ReSharper restore StringLiteralTypo
                _ctlFile, false),
            _loggerFactory.CreateLogger<OVSDBProcess>());

        yield return new NorthDProcess(_sysEnv,
            new NorthDSettings(_ovnSettings.NorthDBConnection,
                _ovnSettings.SouthDBConnection, false),
            _loggerFactory.CreateLogger<NorthDProcess>());
    }

    protected override EitherAsync<Error, Unit> OnProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
        if (process != _northDBProcess)
        {
            _logger.LogInformation("OVN network controller node - north demon has been started");
            return Unit.Default;
        }

        return WaitForDbSocket(cancellationToken)
            .Bind(_ => InitDB(cancellationToken))
            .Bind(_ => ConfigureController(cancellationToken));
}
    
    private EitherAsync<Error, Unit> ConfigureController(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(10000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovnControl = new OVNControlTool(_sysEnv, _ovnSettings.NorthDBConnection);
        return ovnControl.EnsureChassisInGroup("local", "local", 10,
            cancellationToken: cts.Token);
    }

    private EitherAsync<Error, Unit> InitDB(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(new TimeSpan(0,1,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovnControl = new OVNControlTool(_sysEnv, _ovnSettings.NorthDBConnection);
        return ovnControl.InitDb(cts.Token);
    }

    private EitherAsync<Error, Unit> WaitForDbSocket(CancellationToken cancellationToken)
    {
        
        var timeout = new CancellationTokenSource(new TimeSpan(0,1,0));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        async Task<Either<Error, Unit>> WaitForDbSocketAsync()
        {
            _logger.LogTrace("OVN network controller node - waiting for north database to be started.");

            return await _ovnSettings.NorthDBConnection.WaitForDbSocket(_sysEnv,cts.Token).MapAsync(r =>
            {
                if (!r)
                    _logger.LogWarning(
                        "OVN network controller node - failed to wait for north database before timeout");
                else
                    _logger.LogInformation("OVN network controller node - north database has been started.");

                return Unit.Default;
            });
        }

        return WaitForDbSocketAsync().ToAsync();
    }
}