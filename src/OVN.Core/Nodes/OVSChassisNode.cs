using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVSChassisNode : OVSNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));
    
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceManager _ovsDBServiceManager;
    private readonly IServiceManager _vSwitchDBServiceManager;

    private readonly IOVNSettings _ovnSettings;
    // runs: OVSDB, VSwitchD, OVNController
    // connects to OVNDatabaseNode

    private readonly ISysEnvironment _sysEnv;

    public OVSChassisNode(ISysEnvironment sysEnv,
        IOVNSettings ovnSettings,
        ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _ovnSettings = ovnSettings;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<OVNChassisNode>();
        _ovsDBServiceManager = _sysEnv.GetServiceManager("ovsdb");
        _vSwitchDBServiceManager = _sysEnv.GetServiceManager("vswitchd");
    }

    public override EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        EitherAsync<Error, Unit> CreateDBService()
        {
            var ovsdbProcess = GetOvsDBProcess();
            
            return ovsdbProcess.EnsureDBFileCreated()
                .Bind(_ => _ovsDBServiceManager.CreateService(
                        "Open VSwitch Database", ovsdbProcess.GetServiceCommand(), cancellationToken)
                    .Bind(_ => _ovsDBServiceManager.EnsureServiceStarted(cancellationToken)));
        }
        
        EitherAsync<Error, Unit> CreateVSwitchService()
        {
            var vSwitchProcess = GetVSwitchProcess();
            
            return _vSwitchDBServiceManager.CreateService(
                        "Open VSwitch", vSwitchProcess.GetServiceCommand(), cancellationToken)
                    .Bind(_ => _vSwitchDBServiceManager.EnsureServiceStarted(cancellationToken));
        }
        
        EitherAsync<Error, Unit> UpdateService(DemonProcessBase process, IServiceManager serviceManager)
        {
            var validCommand = process.GetServiceCommand();
            
            return serviceManager.GetServiceCommand()
                .Bind(command => Prelude.Cond<string>(c => c != validCommand)
                    .Then(serviceManager.UpdateService(command, cancellationToken))
                    .Else(Unit.Default)
                    (command)
                    .Bind(_ => serviceManager.EnsureServiceStarted(cancellationToken)));
        }
        
        EitherAsync<Error, Unit> UpdateDBService()
            => UpdateService(GetOvsDBProcess(), _ovsDBServiceManager);
        
        EitherAsync<Error, Unit> UpdateVSwitchService()
            => UpdateService(GetVSwitchProcess(), _vSwitchDBServiceManager);
        
        
        return from dbServiceExists in _ovsDBServiceManager.ServiceExists()
            from udb in Prelude.Cond<bool>(c=>c)
                .Then(CreateDBService)
                .Else(UpdateDBService)
                (dbServiceExists)
            
            from switchServiceExists in _vSwitchDBServiceManager.ServiceExists()
            from uSwitch in Prelude.Cond<bool>(c=>c)
                .Then(CreateVSwitchService)
                .Else(UpdateVSwitchService)
                (switchServiceExists)
            
            select Unit.Default;

    }

    public EitherAsync<Error, Unit> Remove(CancellationToken cancellationToken = default)
    {
        EitherAsync<Error, Unit> RemoveService(IServiceManager serviceManager)
            => serviceManager.RemoveService(cancellationToken);

        return RemoveService(_vSwitchDBServiceManager)
            .Bind(_ => RemoveService(_ovsDBServiceManager));
    }

    private OVSDBProcess GetOvsDBProcess()
    {
        return new OVSDBProcess(_sysEnv,
            new OVSDbSettings(
                LocalOVSConnection,
                new OvsFile("etc/openvswitch", "ovs.db"),
                // ReSharper disable StringLiteralTypo
                new OvsFile("usr/share/openvswitch", "vswitch.ovsschema"),
                // ReSharper restore StringLiteralTypo
                new OvsFile("var/run/openvswitch", "ovs-db.ctl")),
            _loggerFactory.CreateLogger<OVSDBProcess>());
    }
    
    
    private VSwitchDProcess GetVSwitchProcess()
    {
        return new VSwitchDProcess(_sysEnv,
            new VSwitchDSettings(LocalOVSConnection),
            _loggerFactory.CreateLogger<VSwitchDProcess>());
        
    }

    public override EitherAsync<Error, Unit> Stop(CancellationToken cancellationToken = default)
    {
        // nothing, keep everything running
        return Unit.Default;
    }

    private EitherAsync<Error, Unit> InitDB(CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(10000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var ovsControl = new OVSControlTool(_sysEnv, LocalOVSConnection);
        return ovsControl.InitDb(cts.Token);
    }
    
}

