namespace Dbosoft.OVNAgent;

public class ClusterPlanConfig
{
    public IList<ChassisGroupConfig> ChassisGroups { get; set; } = new List<ChassisGroupConfig>();
}

public class ChassisGroupConfig
{
    public required string Name { get; init; }

    public IList<ChassisConfig> Chassis { get; set; } = new List<ChassisConfig>();
}

public class ChassisConfig
{
    public required string Name { get; init; }

    public short? Priority { get; set; }
}
