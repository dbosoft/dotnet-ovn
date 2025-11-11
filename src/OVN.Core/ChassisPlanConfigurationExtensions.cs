using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;

namespace Dbosoft.OVN;

public static class ChassisPlanConfigurationExtensions
{
    public static ChassisPlan SetSouthboundDatabase(
        this ChassisPlan chassisPlan,
        IPAddress ipAddress,
        int port = 6642,
        bool ssl = false) =>
        chassisPlan with
        {
            SouthboundDatabase = new OvsDbConnection(ipAddress.ToString(), port, ssl)
        };

    public static ChassisPlan AddGeneveTunnelEndpoint(
        this ChassisPlan chassisPlan,
        IPAddress ipAddress) =>
        chassisPlan with
        {
            TunnelEndpoints = chassisPlan.TunnelEndpoints.Add(new PlannedTunnelEndpoint("geneve", ipAddress)),
        };

    public static ChassisPlan AddVxlanTunnelEndpoint(
        this ChassisPlan chassisPlan,
        IPAddress ipAddress) =>
        chassisPlan with
        {
            TunnelEndpoints = chassisPlan.TunnelEndpoints.Add(new PlannedTunnelEndpoint("vxlan", ipAddress)),
        };

    public static ChassisPlan AddBridgeMapping(
        this ChassisPlan chassisPlan,
        string networkName,
        string bridgeName) =>
        chassisPlan with
        {
            BridgeMappings = chassisPlan.BridgeMappings.Add(networkName, bridgeName),
        };

    public static ChassisPlan AddBridgeMapping(
        this ChassisPlan chassisPlan,
        HashMap<string, string> bridgeMappings) =>
        chassisPlan with
        {
            BridgeMappings = chassisPlan.BridgeMappings + bridgeMappings,
        };
}
