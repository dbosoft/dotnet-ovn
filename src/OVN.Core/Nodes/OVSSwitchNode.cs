using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.OSCommands.OVN;
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

    private VSwitchDProcess? _vSwitchDProcess;

    public OVSSwitchNode(ISysEnvironment sysEnv,
        ILoggerFactory loggerFactory)
    {
        _sysEnv = sysEnv;
        _loggerFactory = loggerFactory;
      }

    protected override IEnumerable<DemonProcessBase> SetupDemons()
    {
        _vSwitchDProcess = new VSwitchDProcess(_sysEnv,
            new VSwitchDSettings(LocalOVSConnection),
            _loggerFactory.CreateLogger<VSwitchDProcess>());

        yield return _vSwitchDProcess;
        
    }
    
}