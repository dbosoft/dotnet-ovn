using Dbosoft.OVN.Model.OVN;
using LanguageExt;

namespace Dbosoft.OVN;

public record ClusterPlan
{
    public HashMap<string, PlannedChassisGroup> PlannedChassisGroups { get; init; }

    public HashMap<string, PlannedChassis> PlannedChassis { get; init; }
}
