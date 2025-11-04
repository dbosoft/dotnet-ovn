using Dbosoft.OVN.Model.OVN;
using LanguageExt;

namespace Dbosoft.OVN;

public record NetworkPlan(string Id)
{
    public HashMap<string, PlannedDnsRecords> PlannedDnsRecords { get; init; }

    public HashMap<string, PlannedDHCPOptions> PlannedDHCPOptions { get;  init; }

    public HashMap<string, PlannedRouterPort> PlannedRouterPorts { get;  init; }

    public HashMap<string, PlannedSwitchPort> PlannedSwitchPorts { get;  init; }

    public HashMap<string, PlannedRouter> PlannedRouters { get;  init; }

    public HashMap<string, PlannedSwitch> PlannedSwitches { get; init; }

    public HashMap<string, PlannedRouterStaticRoute> PlannedRouterStaticRoutes { get;  init; }

    public HashMap<string, PlannedNATRule> PlannedNATRules { get;  init; }
}