using System.Net;
using Dbosoft.OVN.Model.OVS;
using LanguageExt;

namespace Dbosoft.OVN;

public record ChassisPlan(string ChassisId)
{
    public PlannedSwitchSsl? PlannedSwitchSsl { get; init; }

    public OvsDbConnection SouthboundDatabase { get; init; } = LocalConnections.Southbound;

    public Seq<PlannedTunnelEndpoint> TunnelEndpoints { get; init; }

    /// <summary>
    /// Mapping between OVN physical network names and OVS bridge names.
    /// </summary>
    public HashMap<string, string> BridgeMappings { get; init; }
}

public record PlannedTunnelEndpoint(
    string EncapsulationType,
    IPAddress IpAddress);
