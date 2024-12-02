using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Nodes;

public class OVSSwitchNode : DemonNodeBase
{
    private static readonly OvsDbConnection LocalOVSConnection
        = new(new OvsFile("/var/run/openvswitch", "db.sock"));

    private readonly ILoggerFactory _loggerFactory;
    
    // runs: VSwitchD
    // connects to OVSDbNode

    private readonly ISystemEnvironment _systemEnvironment;
    private readonly IOvsSettings _ovsSettings;
    private readonly ILogger _logger;

    private VSwitchDProcess? _vSwitchDProcess;
    private VSwitchDProcess? _fallBackvSwitchDProcess;

    public OVSSwitchNode(
        ISystemEnvironment systemEnvironment,
        IOvsSettings ovsSettings,
        ILoggerFactory loggerFactory)
    {
        _systemEnvironment = systemEnvironment;
        _ovsSettings = ovsSettings;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<OVSSwitchNode>();
    }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        _vSwitchDProcess = new VSwitchDProcess(_systemEnvironment,
            new VSwitchDSettings(LocalOVSConnection, _ovsSettings.Logging, true),
            false,
            _loggerFactory.CreateLogger<VSwitchDProcess>());

        yield return _vSwitchDProcess;
        Thread.Sleep(1000);
        
        _fallBackvSwitchDProcess = new VSwitchDProcess(_systemEnvironment,
            new VSwitchDSettings(LocalOVSConnection, _ovsSettings.Logging, true),
            true,
            _loggerFactory.CreateLogger<VSwitchDProcess>());
        
        yield return _fallBackvSwitchDProcess;
    }

    public override EitherAsync<Error, Unit> Start(
        CancellationToken cancellationToken = default) =>
        from _1 in RightAsync<Error, Unit>(unit)
        let extensionManager = _systemEnvironment.GetOvsExtensionManager()
        from isExtensionEnabled in extensionManager.IsExtensionEnabled()
        from _2 in isExtensionEnabled
            ? base.Start(cancellationToken)
                .MapLeft(l =>
                {
                    _logger.LogError(l, "Failed to start vswitch daemon.");
                    return l;
                })
            : RightAsync<Error, Unit>(unit)
        select unit;

    public override EitherAsync<Error, Unit> EnsureAlive(
        bool checkResponse,
        CancellationToken cancellationToken = default) =>
        from _1 in RightAsync<Error, Unit>(unit)
        let extensionManager = _systemEnvironment.GetOvsExtensionManager()
        from isExtensionEnabled in extensionManager.IsExtensionEnabled()
        from _2 in isExtensionEnabled
            ? base.EnsureAlive(checkResponse, cancellationToken)
            : from _1 in Optional(_vSwitchDProcess)
                             .Map(p => p.Stop(false, cancellationToken))
                             .SequenceSerial()
              from _2 in Optional(_fallBackvSwitchDProcess)
                             .Map(p => p.Stop(false, cancellationToken))
                             .SequenceSerial()
              select unit
        select unit;
}
