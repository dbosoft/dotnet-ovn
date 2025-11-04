using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

public class ClusterPlanRealizer : PlanRealizer
{
    private readonly ILogger _logger;
    private readonly IOVSDBTool _ovnDBTool;

    public ClusterPlanRealizer(IOVSDBTool ovnDBTool, ILogger logger) : base(ovnDBTool, logger)
    {
        _ovnDBTool = ovnDBTool;
        _logger = logger;
    }

    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from existingChassis in FindRecords<Chassis>(
            OVNTableNames.Chassis,
            Chassis.Columns,
            cancellationToken: cancellationToken)
        from remainingChassis in RemoveEntitiesNotPlanned(
            OVNTableNames.Chassis,
            existingChassis,
            clusterPlan.PlannedChassis,
            cancellationToken: cancellationToken)
                from existingChassisGroups in FindRecords<ChassisGroup>(
            OVNTableNames.ChassisGroups,
            ChassisGroup.Columns,
            cancellationToken: cancellationToken)
        from remainingChassisGroups in RemoveEntitiesNotPlanned(
            OVNTableNames.ChassisGroups,
            existingChassisGroups,
            clusterPlan.PlannedChassisGroups,
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
        from _3 in UpdateEntities(
            OVNTableNames.ChassisGroups,
            remainingChassisGroups,
            existingPlannedChassisGroups,
            cancellationToken: cancellationToken)
        from _4 in UpdateEntities(
            OVNTableNames.Chassis,
            remainingChassis,
            existingPlannedChassis,
            cancellationToken: cancellationToken)
        select clusterPlan;
}
