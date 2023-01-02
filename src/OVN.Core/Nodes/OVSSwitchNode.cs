using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

public class OVSSwitchNode : DemonNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));

    private readonly ILoggerFactory _loggerFactory;
    
    // runs: VSwitchD
    // connects to OVSDbNode

    private readonly ISysEnvironment _sysEnv;
    private readonly ILogger _logger;

    private VSwitchDProcess? _vSwitchDProcess;

    public OVSSwitchNode(ISysEnvironment sysEnv,
        ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<OVSSwitchNode>();
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        _vSwitchDProcess = new VSwitchDProcess(_sysEnv,
            new VSwitchDSettings(LocalOVSConnection),
            _loggerFactory.CreateLogger<VSwitchDProcess>());

        yield return _vSwitchDProcess;
        
    }
    public override EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        var extensionManager = _sysEnv.GetOvsExtensionManager();
        
        if(extensionManager.IsExtensionEnabled()) 
            base.Start(cancellationToken).IfLeft(l => _logger.LogError("Failed to start vswitch demon. Error: {@error}", l));

        return Unit.Default;
    }
    
    public override EitherAsync<Error, Unit> EnsureAlive(bool checkResponse, CancellationToken cancellationToken = default)
    {
        var extensionManager = _sysEnv.GetOvsExtensionManager();
        return !extensionManager.IsExtensionEnabled() 
            ? _vSwitchDProcess?.Stop(false, cancellationToken) ?? Unit.Default
            : base.EnsureAlive(checkResponse, cancellationToken);
    }
    
}