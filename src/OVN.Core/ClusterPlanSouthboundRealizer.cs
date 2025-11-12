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
        from existingChassis in FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection,
            SouthboundConnection.Columns,
            cancellationToken: cancellationToken)
        from remainingConnections in RemoveEntitiesNotPlanned(
            OVNSouthboundTableNames.Connection,
            existingChassis,
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
