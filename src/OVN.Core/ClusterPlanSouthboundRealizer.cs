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
    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from _1 in ApplySouthboundConnections(clusterPlan, cancellationToken)
        from _2 in ApplySsl<PlannedSouthboundSsl, SouthboundSsl, SouthboundGlobal>(
            clusterPlan.PlannedSouthboundSsl,
            OVNSouthboundTableNames.Global,
            cancellationToken)
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
}
