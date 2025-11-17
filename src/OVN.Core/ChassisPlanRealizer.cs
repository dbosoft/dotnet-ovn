using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVS;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class ChassisPlanRealizer : PlanRealizer
{
    private readonly ISystemEnvironment _systemEnvironment;
    private readonly IOVSDBTool _ovsDBTool;

    public ChassisPlanRealizer(
        ISystemEnvironment systemEnvironment,
        IOVSDBTool ovsDBTool)
        : base(systemEnvironment, ovsDBTool)
    {
        _systemEnvironment = systemEnvironment;
        _ovsDBTool = ovsDBTool;
    }
    
    public EitherAsync<Error, ChassisPlan> ApplyChassisPlan(
        ChassisPlan chassisPlan,
        CancellationToken cancellationToken = default) =>
        from _1 in ApplyOvnConfiguration(chassisPlan, cancellationToken)
        from _2 in ApplySsl<PlannedSwitchSsl, SwitchSsl, SwitchGlobal>(
            chassisPlan.PlannedSwitchSsl,
            OVSTableNames.Global,
            cancellationToken)
        select chassisPlan;

    private EitherAsync<Error, Unit> ApplyOvnConfiguration(
        ChassisPlan chassisPlan,
        CancellationToken cancellationToken = default) =>
        from optionalOvsRecord in _ovsDBTool.GetRecord<SwitchGlobal>(
            OVSTableNames.Global,
            ".",
            cancellationToken: cancellationToken)
        from ovsRecord in optionalOvsRecord
            .ToEitherAsync(Error.New("The OVS configuration table does not exist."))
        let chassisId = ovsRecord.ExternalIds.Find("system-id")
        from _1 in chassisId
            .Filter(i => i != chassisPlan.ChassisId)
            .Map<EitherAsync<Error, Unit>>(i =>
                Error.New($"A different chassis ID ('{i}') is already configured. The chassis ID cannot be changed."))
            .Sequence()
        let bridgeMappings = string.Join(",",
            chassisPlan.BridgeMappings.ToSeq().Map(kvp => $"{kvp.Key}:{kvp.Value}").Order())
        let encapTypes = string.Join(",", chassisPlan.TunnelEndpoints.Map(e => e.EncapsulationType))
        let encapIps = string.Join(",", chassisPlan.TunnelEndpoints.Map(e => e.IpAddress))
        let ovnRemote = chassisPlan.SouthboundDatabase
            .GetCommandString(_systemEnvironment.FileSystem, false)
        let update = Map(
            ("system-id", OVSValue<string>.New(chassisPlan.ChassisId)),
            ("ovn-bridge-mappings", OVSValue<string>.New(bridgeMappings)),
            ("ovn-encap-type", OVSValue<string>.New(encapTypes)),
            ("ovn-encap-ip", OVSValue<string>.New(encapIps)),
            ("ovn-remote", OVSValue<string>.New(ovnRemote)))
        from _2 in _ovsDBTool.UpdateColumnKeyValues(
            OVSTableNames.Global, ".", "external-ids", update, cancellationToken)
        select unit;
}
