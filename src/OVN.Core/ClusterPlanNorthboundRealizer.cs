using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class ClusterPlanNorthboundRealizer(
    ISystemEnvironment systemEnvironment,
    IOVSDBTool ovnDBTool)
    : PlanRealizer(systemEnvironment, ovnDBTool)
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
        from _3 in ApplyNorthboundConnections(clusterPlan, cancellationToken)
        from _4 in ApplySsl<PlannedNorthboundSsl, NorthboundSsl, NorthboundGlobal>(
            clusterPlan.PlannedNorthboundSsl,
            OVNTableNames.Global,
            cancellationToken)
        select clusterPlan;

    private EitherAsync<Error, Unit> ApplyNorthboundConnections(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken) =>
        from global in FindRecords<NorthboundGlobal>(
            OVNTableNames.Global,
            NorthboundGlobal.Columns,
            cancellationToken: cancellationToken)
        from existingConnection in FindRecordsWithParents<NorthboundConnection, NorthboundGlobal>(
            OVNTableNames.Connection,
            global.Values.ToSeq(),
            NorthboundConnection.Columns,
            cancellationToken: cancellationToken)
        from remainingConnections in RemoveEntitiesNotPlanned(
            OVNTableNames.Connection,
            existingConnection,
            clusterPlan.PlannedNorthboundConnections,
            cancellationToken: cancellationToken)
        from existingPlannedConnections in CreatePlannedEntities(
            OVNTableNames.Connection,
            remainingConnections,
            clusterPlan.PlannedNorthboundConnections,
            cancellationToken: cancellationToken)
        from _1 in UpdateEntities(
            OVNTableNames.Connection,
            remainingConnections,
            existingPlannedConnections,
            cancellationToken: cancellationToken)
        select unit;
}
