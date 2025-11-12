using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN;

public class ClusterPlanNorthboundRealizer(IOVSDBTool ovnDBTool, ILogger logger)
    : PlanRealizer(ovnDBTool, logger)
{
    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from existingChassisGroups in FindRecords<ChassisGroup>(
            OVNTableNames.ChassisGroups,
            ChassisGroup.Columns,
            cancellationToken: cancellationToken)
        from remainingChassisGroups in RemoveEntitiesNotPlanned(
            OVNTableNames.ChassisGroups,
            existingChassisGroups,
            clusterPlan.PlannedChassisGroups,
            cancellationToken: cancellationToken)
        from existingChassis in FindRecordsWithParents<Chassis, ChassisGroup>(
            OVNTableNames.Chassis,
            existingChassisGroups.Values.ToSeq(),
            Chassis.Columns,
            cancellationToken: cancellationToken)
        from remainingChassis in RemoveEntitiesNotPlanned(
            OVNTableNames.Chassis,
            existingChassis,
            clusterPlan.PlannedChassis,
            cancellationToken: cancellationToken)
        from existingPlannedChassisGroups in CreatePlannedEntities(
            OVNTableNames.ChassisGroups,
            remainingChassisGroups,
            clusterPlan.PlannedChassisGroups,
            cancellationToken: cancellationToken)
        from existingPlannedChassis in CreatePlannedEntities(
            OVNTableNames.Chassis,
            remainingChassis,
            clusterPlan.PlannedChassis,
            cancellationToken: cancellationToken)
        from _1 in UpdateEntities(
            OVNTableNames.ChassisGroups,
            remainingChassisGroups,
            existingPlannedChassisGroups,
            cancellationToken: cancellationToken)
        from _2 in UpdateEntities(
            OVNTableNames.Chassis,
            remainingChassis,
            existingPlannedChassis,
            cancellationToken: cancellationToken)
        select clusterPlan;
}
