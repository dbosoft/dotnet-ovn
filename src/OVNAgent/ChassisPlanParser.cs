using System.Net;
using Dbosoft.OVN;

namespace Dbosoft.OVNAgent;

public static class ChassisPlanParser
{
    public static ChassisPlan ParseYaml(string yaml)
    {
        var planConfig = PlanYamlSerializer.Deserialize<ChassisPlanConfig>(yaml);
        
        // TODO handle southbound database config

        if (string.IsNullOrWhiteSpace(planConfig.Name))
            throw new InvalidDataException("The chassis name is required.");

        var clusterPlan = new ChassisPlan(planConfig.Name)
            .AddBridgeMapping(planConfig.BridgeMappings.ToHashMap());

        foreach (var tunnelEndpointConfig in planConfig.TunnelEndpoints)
        {
            clusterPlan = ParseTunnelEndpoint(clusterPlan, tunnelEndpointConfig);
        }

        return clusterPlan;
    }

    private static ChassisPlan ParseTunnelEndpoint(
        ChassisPlan chassisPlan,
        TunnelEndpointConfig tunnelEndpointConfig)
    { 
        if (!IPAddress.TryParse(tunnelEndpointConfig.IpAddress, out var endpointAddress))
            throw new InvalidDataException("The tunnel endpoint must be a valid IP address.");

        return tunnelEndpointConfig.EncapsulationType switch
        {
            "geneve" => chassisPlan.AddGeneveTunnelEndpoint(endpointAddress),
            "vxlan" => chassisPlan.AddVxlanTunnelEndpoint(endpointAddress),
            _ => throw new InvalidDataException("The encapsulation type must be 'geneve' and 'vxlan'."),
        };
    }
}
