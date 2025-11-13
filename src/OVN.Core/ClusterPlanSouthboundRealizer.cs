using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class ClusterPlanSouthboundRealizer(
    ISystemEnvironment systemEnvironment,
    IOVSDBTool ovnDBTool)
    : PlanRealizer(systemEnvironment, ovnDBTool)
{
    // Set SSL protocols"ssl_protocols": "'"TLSv1.3,TLSv1.2"'",

    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from _1 in ApplySouthboundConnections(clusterPlan, cancellationToken)
        from _2 in ApplySouthboundSsl(clusterPlan, cancellationToken)
        select clusterPlan;


    private EitherAsync<Error, Unit> ApplySouthboundConnections(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken) =>
        from global in FindRecords<SouthboundGlobal>(
            OVNSouthboundTableNames.Global,
            SouthboundGlobal.Columns,
            cancellationToken: cancellationToken)
        from existingConnection in FindRecordsWithParents<SouthboundConnection, SouthboundGlobal>(
            OVNSouthboundTableNames.Connection,
            global.Values.ToSeq(),
            SouthboundConnection.Columns,
            cancellationToken: cancellationToken)
        from remainingConnections in RemoveEntitiesNotPlanned(
            OVNSouthboundTableNames.Connection,
            existingConnection,
            clusterPlan.PlannedSouthboundConnections,
            cancellationToken: cancellationToken)
        from existingPlannedChassisGroups in CreatePlannedEntities(
            OVNSouthboundTableNames.Connection,
            remainingConnections,
            clusterPlan.PlannedSouthboundConnections,
            cancellationToken: cancellationToken)
        from _3 in UpdateEntities(
            OVNSouthboundTableNames.Connection,
            remainingConnections,
            existingPlannedChassisGroups,
            cancellationToken: cancellationToken)
        select unit;

    // The SSL configuration is special there can be at most one SSL

    // TODO Map certificates
    private EitherAsync<Error, Unit> ApplySouthboundSsl(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken) =>
        from global in FindRecords<SouthboundGlobal>(
            OVNSouthboundTableNames.Global,
            SouthboundGlobal.Columns,
            cancellationToken: cancellationToken)
        from sslWithPaths in EnsureCertificateFiles(
            clusterPlan.PlannedSouthboundSsl,
            cancellationToken)
        let plannedSsl = sslWithPaths
            .Map(s => (s.Name, s))
            .ToHashMap()
        from existingSsl in FindRecordsWithParents<SouthboundSsl, SouthboundGlobal>(
            OVNSouthboundTableNames.Ssl,
            global.Values.ToSeq(),
            SouthboundSsl.Columns,
            cancellationToken: cancellationToken)
        from remainingSsl in RemoveEntitiesNotPlanned(
            OVNSouthboundTableNames.Ssl,
            existingSsl,
            plannedSsl,
            cancellationToken: cancellationToken)
        from existingPlannedSsl in CreatePlannedEntities(
            OVNSouthboundTableNames.Ssl,
            remainingSsl,
            plannedSsl,
            cancellationToken: cancellationToken)
        from _3 in UpdateEntities(
            OVNSouthboundTableNames.Ssl,
            remainingSsl,
            existingPlannedSsl,
            cancellationToken: cancellationToken)
        select unit;
}
