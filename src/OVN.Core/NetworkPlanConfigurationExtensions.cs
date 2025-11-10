using System.Net;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public static class NetworkPlanConfigurationExtensions
{
    public static NetworkPlan AddSwitch(
        this NetworkPlan plan,
        string switchName,
        Seq<string> dnsRecordsNames = default)
    {
        return plan with
        {
            PlannedSwitches = plan.PlannedSwitches.Add(switchName, new PlannedSwitch
            {
                Name = switchName,
                ExternalIds = Map(
                    ("network_plan", plan.Id),
                    ("dns_records", string.Join(',', dnsRecordsNames))),
            })
        };
    }
    
    public static NetworkPlan AddDnsRecords(
        this NetworkPlan plan,
        string id,
        Map<string, string> records,
        Map<string, string> options)
    {
        return plan with
        {
            PlannedDnsRecords = plan.PlannedDnsRecords.Add(id, new PlannedDnsRecords
            {
                Records = records,
                Options = options,
                ExternalIds = Map(
                    ("network_plan", plan.Id), 
                    ( "id", id)),
            })
        };
    }

    public static NetworkPlan AddDHCPOptions(
        this NetworkPlan plan,
        string id,
        IPNetwork2 cidr,
        Map<string, string> options)
    {
        return plan with
        {
            PlannedDHCPOptions = plan.PlannedDHCPOptions.Add(id, new PlannedDHCPOptions
            {
                Cidr = cidr.ToString(),
                Options = options,
                ExternalIds = Map(
                    ("network_plan", plan.Id),
                    ("id", id)),
            })
        };
    }

    public static NetworkPlan AddExternalNetworkPort(
        this NetworkPlan plan,
        string switchName,
        string externalNetwork,
        int? tag)
    {
        var name = $"SN-{switchName}-{externalNetwork}";
        
        return plan with
        {
            PlannedSwitchPorts = plan.PlannedSwitchPorts.Add(name, new PlannedSwitchPort(switchName)
            {
                Name = name,
                Type = "localnet",
                Options = Map(("network_name", externalNetwork)),
                Addresses = Seq1("unknown"),
                ExternalIds = Map(("network_plan", plan.Id)),
                Tag = tag,
            })
        };
    }

    public static NetworkPlan AddNetworkPort(
        this NetworkPlan plan, 
        string switchName, 
        string portName,
        string macAddress,
        IPAddress address,
        string dhcpOptionsV4 = "")
    {
        return plan with
        {
            PlannedSwitchPorts = plan.PlannedSwitchPorts.Add(portName, new PlannedSwitchPort(switchName)
            {
                Name = portName,
                Addresses = Seq1($"{macAddress} {address}"),
                PortSecurity = Seq1($"{macAddress} {address}"),
                ExternalIds = Map(
                    ("network_plan", plan.Id),
                    ("dhcp_options_v4", dhcpOptionsV4)),
            })
        };
    }

    public static NetworkPlan AddRouter(
        this NetworkPlan plan,
        string routerName)
    {
        return plan with
        {
            PlannedRouters = plan.PlannedRouters.Add(routerName, new PlannedRouter
            {
                Name = routerName,
                ExternalIds = Map(("network_plan", plan.Id)),
            })
        };
    }

    public static NetworkPlan AddNATRule(
        this NetworkPlan plan,
        string routerName,
        string type,
        IPAddress externalIP,
        string externalMAC,
        string logicalIP,
        string logicalPort = "")
    {
        var natRule = new PlannedNATRule(routerName)
        {
            Type = type,
            ExternalIP = externalIP.ToString(),
            ExternalMAC = externalMAC,
            LogicalIP = logicalIP,
            LogicalPort = logicalPort,
            ExternalIds = Map(
                ("network_plan", plan.Id),
                ("router_name", routerName)),
        };

        return plan with { PlannedNATRules = plan.PlannedNATRules.Add(natRule.Name, natRule) };
    }

    public static NetworkPlan AddSourceNATRule(
        this NetworkPlan plan,
        string routerName,
        IPAddress externalIP,
        IPNetwork2 network)
    {
        var natRule = new PlannedNATRule(routerName)
        {
            Type = "snat",
            ExternalIP = externalIP.ToString(),
            LogicalIP = network.ToString(),
            ExternalIds = Map(
                ("network_plan", plan.Id),
                ("router_name", routerName)),
        };

        return plan with
        {
            PlannedNATRules = plan.PlannedNATRules.Add(natRule.Name, natRule)
        };
    }

    public static NetworkPlan AddDestinationNATRule(
        this NetworkPlan plan,
        string routerName,
        IPAddress externalIP,
        string externalMac,
        IPAddress logicalIP,
        string logicalPort = "")
    {
        var natRule = new PlannedNATRule(routerName)
        {
            Type = "dnat_and_snat",
            ExternalIP = externalIP.ToString(),
            ExternalMAC = externalMac,
            LogicalIP = logicalIP.ToString(),
            LogicalPort = logicalPort,
            ExternalIds = Map(
                ("network_plan", plan.Id),
                ("router_name", routerName)),
        };

        return plan with
        {
            PlannedNATRules = plan.PlannedNATRules.Add(natRule.Name, natRule)
        };
    }

    public static NetworkPlan AddStaticRoute(
        this NetworkPlan plan,
        string routerName,
        string ipPrefix,
        IPAddress nextHop,
        string routeTable = "")
    {
        var route = new PlannedRouterStaticRoute(routerName)
        {
            IpPrefix = ipPrefix,
            NextHop = nextHop.ToString(),
            RouteTable = routeTable,
            ExternalIds = Map(
                ("network_plan", plan.Id),
                ("router_name", routerName)),
        };
        
        return plan with { PlannedRouterStaticRoutes = plan.PlannedRouterStaticRoutes.Add(route.Name, route) };
    }

    public static NetworkPlan AddRouterPort(
        this NetworkPlan plan,
        string switchName, 
        string routerName,
        string macAddress,
        IPAddress ipAddress,
        IPNetwork2 network,
        string chassisGroup = "",
        string routeTable = "")
    {
        var portNameRouter = $"RS-{routerName}-{switchName}";
        var portNameSwitch = $"SR-{switchName}-{routerName}";

        return plan with
        {
            PlannedRouterPorts = plan.PlannedRouterPorts.Add(portNameRouter, new PlannedRouterPort(routerName)
            {
                Name = portNameRouter,
                MacAddress = macAddress,
                Networks = Seq1($"{ipAddress}/{network.Cidr}"),
                ExternalIds = string.IsNullOrWhiteSpace(chassisGroup)
                    ? Map(("network_plan", plan.Id))
                    : Map(("network_plan", plan.Id), ("chassis_group", chassisGroup)),
                Options = string.IsNullOrWhiteSpace(routeTable)
                    ? Map<string, string>()
                    : Map(("route_table", routeTable)),
            }),
            PlannedSwitchPorts = plan.PlannedSwitchPorts.Add(portNameSwitch, new PlannedSwitchPort(switchName)
            {
                Name = portNameSwitch,
                Type = "router",
                Addresses = Seq1("router"),
                Options = Map(
                    ("router-port", portNameRouter),
                    ("nat-addresses", "router")),
                ExternalIds = Map(("network_plan", plan.Id)),
            }),
        };
    }

    public static NetworkPlan AddRouterPeerPort(
        this NetworkPlan plan,
        string routerName,
        string otherRouterName,
        string macAddress,
        IPAddress ipAddress,
        IPNetwork2 network)
    {
        var portName = $"RR-{routerName}-{otherRouterName}";
        var otherPortName = $"RR-{otherRouterName}-{routerName}";

        return plan with
        {
            PlannedRouterPorts = plan.PlannedRouterPorts.Add(portName, new PlannedRouterPort(routerName)
            {
                Name = portName,
                MacAddress = macAddress,
                Networks = Seq1($"{ipAddress}/{network.Cidr}"),
                Peer = otherPortName,
                ExternalIds = Map(("network_plan", plan.Id)),
            }),
        };
    }
}
