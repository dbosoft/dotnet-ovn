using Dbosoft.OVN;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVNAgent;

public class ChassisPlanConfig
{
    public required string Name { get; init; }

    public IList<TunnelEndpointConfig> TunnelEndpoints { get; init; } = new List<TunnelEndpointConfig>();

    public IDictionary<string, string> BridgeMappings { get; set; } = new Dictionary<string, string>();
}

public class TunnelEndpointConfig
{
    public required string EncapsulationType { get; init; }

    public required string IpAddress { get; init; }
}
