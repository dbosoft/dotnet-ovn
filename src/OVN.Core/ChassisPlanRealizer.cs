using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class ChassisPlanRealizer : PlanRealizer
{
    private readonly ISystemEnvironment _systemEnvironment;
    private readonly IOVSDBTool _ovsDBTool;

    public ChassisPlanRealizer(
        ISystemEnvironment systemEnvironment,
        IOVSDBTool ovsDBTool,
        ILogger logger)
        : base(ovsDBTool, logger)
    {
        _systemEnvironment = systemEnvironment;
        _ovsDBTool = ovsDBTool;
    }
    
    public EitherAsync<Error, ChassisPlan> ApplyChassisPlan(
        ChassisPlan chassisPlan,
        CancellationToken cancellationToken = default) =>
        from optionalOvsRecord in _ovsDBTool.GetRecord<OVSTableRecord>(
            "Open_vSwitch", ".",
            cancellationToken: cancellationToken)
        from ovsRecord in optionalOvsRecord
            .ToEitherAsync(Error.New("The OVS configuration table does not exist."))
        let chassisId = ovsRecord.ExternalIds.Find("system-id")
        from _1 in chassisId
            .Filter(i => i != chassisPlan.ChassisId)
            .Map<EitherAsync<Error, Unit>>(i => Error.New($"A different chassis ID ('{i}') is already configured. The chassis ID cannot be changed."))
            .Sequence()
        let bridgeMappings = string.Join(",", chassisPlan.BridgeMappings.ToSeq().Map(kvp => $"{kvp.Key}:{kvp.Value}").Order())
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
            "Open_vSwitch", ".", "external-ids", update, cancellationToken)
        select chassisPlan;
}
