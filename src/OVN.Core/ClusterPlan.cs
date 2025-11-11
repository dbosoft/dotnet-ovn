using Dbosoft.OVN.Model.OVN;
using Dbosoft.OVN.Model.OVS;
using LanguageExt;

namespace Dbosoft.OVN;

public record ClusterPlan
{
    public HashMap<string, PlannedChassisGroup> PlannedChassisGroups { get; init; }

    public HashMap<string, PlannedChassis> PlannedChassis { get; init; }

    public HashMap<string, PlannedSouthboundConnection> PlannedSouthboundConnections { get; init; }
}
