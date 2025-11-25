using System.Net;
using Dbosoft.OVN.Model.OVS;
using LanguageExt;

namespace Dbosoft.OVN;

public static class ChassisPlanConfigurationExtensions
{
    public static ChassisPlan SetSouthboundDatabase(
        this ChassisPlan plan,
        IPAddress ipAddress,
        int port = 6642,
        bool ssl = false) =>
        plan with
        {
            SouthboundDatabase = new OvsDbConnection(ipAddress.ToString(), port, ssl)
        };

    public static ChassisPlan SetSwitchSsl(
        this ChassisPlan plan,
        string privateKey,
        string certificate,
        string caCertificate) =>
        plan with
        {
            PlannedSwitchSsl = new PlannedSwitchSsl
            {
                PrivateKey = privateKey,
                Certificate = certificate,
                CaCertificate = caCertificate
            },
        };

    public static ChassisPlan AddGeneveTunnelEndpoint(
        this ChassisPlan plan,
        IPAddress ipAddress) =>
        plan with
        {
            TunnelEndpoints = plan.TunnelEndpoints.Add(new PlannedTunnelEndpoint("geneve", ipAddress)),
        };

    public static ChassisPlan AddVxlanTunnelEndpoint(
        this ChassisPlan plan,
        IPAddress ipAddress) =>
        plan with
        {
            TunnelEndpoints = plan.TunnelEndpoints.Add(new PlannedTunnelEndpoint("vxlan", ipAddress)),
        };

    public static ChassisPlan AddBridgeMapping(
        this ChassisPlan plan,
        string networkName,
        string bridgeName) =>
        plan with
        {
            BridgeMappings = plan.BridgeMappings.Add(networkName, bridgeName),
        };

    public static ChassisPlan AddBridgeMapping(
        this ChassisPlan plan,
        HashMap<string, string> bridgeMappings) =>
        plan with
        {
            BridgeMappings = plan.BridgeMappings + bridgeMappings,
        };
}
