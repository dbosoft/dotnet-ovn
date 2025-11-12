using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN;

public class ClusterPlanSouthboundRealizer(IOVSDBTool ovnDBTool, ILogger logger)
    : PlanRealizer(ovnDBTool, logger)
{
    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
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
        select clusterPlan;
}
