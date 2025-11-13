using System.Net;
using Dbosoft.OVN;

namespace Dbosoft.OVNAgent;

public static class ChassisPlanParser
{
    public static ChassisPlan ParseYaml(string yaml)
    {
        var planConfig = PlanYamlSerializer.Deserialize<ChassisPlanConfig>(yaml);

        if (string.IsNullOrWhiteSpace(planConfig.Name))
            throw new InvalidDataException("The chassis name is required.");

        var chassisPlan = new ChassisPlan(planConfig.Name)
            .AddBridgeMapping(planConfig.BridgeMappings.ToHashMap());

        chassisPlan = ParseSouthboundConnection(chassisPlan, planConfig.SouthboundConnection);

        foreach (var tunnelEndpointConfig in planConfig.TunnelEndpoints)
        {
            chassisPlan = ParseTunnelEndpoint(chassisPlan, tunnelEndpointConfig);
        }

        return chassisPlan;
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

    private static ChassisPlan ParseSouthboundConnection(
        ChassisPlan chassisPlan,
        SouthboundConnectionConfig? southboundConnectionConfig)
    {
        if (southboundConnectionConfig is null)
            return chassisPlan;

        if (!IPAddress.TryParse(southboundConnectionConfig.IpAddress, out var ipAddress))
            throw new InvalidDataException("The southbound connection must have a valid IP address.");

        return chassisPlan.SetSouthboundDatabase(
            ipAddress,
            southboundConnectionConfig.Port,
            southboundConnectionConfig.Ssl.GetValueOrDefault());
    }
}
