using System.Net;
using Dbosoft.OVN;

namespace Dbosoft.OVNAgent;

public static class ClusterPlanParser
{
    public static ClusterPlan ParseYaml(string yaml)
    {
        var planConfig = PlanYamlSerializer.Deserialize<ClusterPlanConfig>(yaml);
        var clusterPlan = new ClusterPlan();

        foreach (var chassisGroupConfig in planConfig.ChassisGroups)
        {
            clusterPlan = ParseChassisGroup(clusterPlan, chassisGroupConfig);
        }

        foreach (var southboundConnectionConfig in planConfig.SouthboundEndpoints)
        {
            clusterPlan = ParseSouthboundConnections(clusterPlan, southboundConnectionConfig);
        }

        clusterPlan = ParseSouthboundSsl(clusterPlan, planConfig.SouthboundSsl);

        return clusterPlan;
    }

    private static ClusterPlan ParseChassisGroup(
        ClusterPlan clusterPlan,
        ChassisGroupConfig chassisGroupConfig)
    {
        if (string.IsNullOrWhiteSpace(chassisGroupConfig.Name))
            throw new InvalidDataException("The chassis group name is required.");
        
        clusterPlan = clusterPlan.AddChassisGroup(chassisGroupConfig.Name);

        foreach (var chassisConfig in chassisGroupConfig.Chassis)
        {
            clusterPlan = ParseChassis(clusterPlan, chassisConfig, chassisGroupConfig.Name);
        }

        return clusterPlan;
    }

    private static ClusterPlan ParseChassis(
        ClusterPlan clusterPlan,
        ChassisConfig chassisConfig,
        string chassisGroupName)
    {
        if (string.IsNullOrWhiteSpace(chassisConfig.Name))
            throw new InvalidDataException("The chassis name is required.");

        return clusterPlan.AddChassis(
            chassisGroupName,
            chassisConfig.Name,
            // The priority can be between 0 and 32,767. We use 16,383 as the default priority.
            chassisConfig.Priority.GetValueOrDefault(16383));
    }

    private static ClusterPlan ParseSouthboundConnections(
        ClusterPlan clusterPlan,
        SouthboundEndpointConfig endpointConfig)
    {
        return clusterPlan.AddSouthboundConnection(
            endpointConfig.Port,
            endpointConfig.Ssl.GetValueOrDefault(),
            string.IsNullOrWhiteSpace(endpointConfig.IpAddress)
                ? null
                : IPAddress.Parse(endpointConfig.IpAddress));
    }

    private static ClusterPlan ParseSouthboundSsl(
        ClusterPlan clusterPlan,
        SouthboundSslConfig? sslConfig)
    {
        return sslConfig is null
            ? clusterPlan
            : clusterPlan.SetSouthboundSsl(
                sslConfig.PrivateKey,
                sslConfig.Certificate,
                sslConfig.CaCertificate);
    }
}
