namespace Dbosoft.OVNAgent;

public class ClusterPlanConfig
{
    public IList<ChassisGroupConfig> ChassisGroups { get; set; } = new List<ChassisGroupConfig>();

    public IList<SouthboundEndpointConfig> SouthboundEndpoints { get; set; } = new List<SouthboundEndpointConfig>();

    public SouthboundSslConfig? SouthboundSsl { get; init; }
}

public class ChassisGroupConfig
{
    public required string Name { get; init; }

    public IList<ChassisConfig> Chassis { get; set; } = new List<ChassisConfig>();
}

public class ChassisConfig
{
    public required string Name { get; init; }

    public short? Priority { get; init; }
}

public class SouthboundEndpointConfig
{
    public required int Port { get; init; }

    public bool? Ssl { get; init; }

    public string? IpAddress { get; init; }
}

public class SouthboundSslConfig
{
    public required string PrivateKey { get; init; }

    public required string Certificate { get; init; }

    public required string CaCertificate { get; init; }
}