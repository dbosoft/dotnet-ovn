using System.Net;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;

namespace Dbosoft.OVN;

public static class NetworkPlanConfigurationExtensions
{

    public static NetworkPlan AddSwitch(this NetworkPlan plan, string switchName, string[]? dnsRecordsNames = default)
    {
        return plan with
        {
            PlannedSwitches = plan.PlannedSwitches.Add(switchName, new PlannedSwitch
            {
                Name = switchName,
                ExternalIds = new Dictionary<string, string> {
                    { "network_plan", plan.Id },
                    {  "dns_records", dnsRecordsNames==default ? "": string.Join(',',dnsRecordsNames)}
                }.ToMap()
            })
        };
    }
    
    public static NetworkPlan AddDnsRecords(this NetworkPlan plan, string id, Map<string, string> records)
    {
        return plan with
        {
            PlannedDnsRecords = plan.PlannedDnsRecords.Add(id, new PlannedDnsRecords
            {
                Records = records,
                ExternalIds = new Dictionary<string, string> {
                    { "network_plan", plan.Id }, 
                    { "id", id} 
                }.ToMap()
            })
        };
    }

    public static NetworkPlan AddDHCPOptions(this NetworkPlan plan, string id, IPNetwork cidr, Map<string, string> options)
    {
        return plan with
        {
            PlannedDHCPOptions =
            plan.PlannedDHCPOptions.Add(id, new PlannedDHCPOptions
            {
                Cidr = cidr.ToString(),
                Options = options,
                ExternalIds = new Dictionary<string, string>
                {
                    { "network_plan", plan.Id },
                    { "id", id }
                }.ToMap()
            })
        };
    }

    public static NetworkPlan AddExternalNetworkPort(this NetworkPlan plan, string switchName, string externalNetwork)
    {
        var name = $"SN-{switchName}-{externalNetwork}";
        return plan with
        {
            PlannedSwitchPorts =
            plan.PlannedSwitchPorts.Add(name, new PlannedSwitchPort(switchName)
            {
                Name = name,
                Type = "localnet",
                Options = new Dictionary<string, string> { { "network_name", externalNetwork } }.ToMap(),
                Addresses = new[] { "unknown" }.ToSeq(),
                ExternalIds = new Dictionary<string, string> { { "network_plan", plan.Id } }.ToMap()
            })
        };
    }

    public static NetworkPlan AddNetworkPort(this NetworkPlan plan, 
        string switchName, 
        string portName, string macAddress, IPAddress address, string dhcpOptionsV4 = "")
    {
        return plan with
        {
            PlannedSwitchPorts =
            plan.PlannedSwitchPorts.Add(portName, new PlannedSwitchPort(switchName)
            {
                Name = portName,
                Addresses = new[] { $"{macAddress} {address}"}.ToSeq(),
                PortSecurity = new[] { $"{macAddress} {address}" }.ToSeq(),
                ExternalIds = new Dictionary<string, string>
                {
                    { "network_plan", plan.Id },
                    { "dhcp_options_v4", dhcpOptionsV4 }
                }.ToMap()
            })
        };
    }

    public static NetworkPlan AddRouter(this NetworkPlan plan, string routerName)
    {
        return plan with
        {
            PlannedRouters =
            plan.PlannedRouters.Add(routerName, new PlannedRouter
            {
                Name = routerName,
                ExternalIds = new Dictionary<string, string> { { "network_plan", plan.Id } }.ToMap()
            })
        };
    }

    public static NetworkPlan AddNATRule(this NetworkPlan plan, string routerName, string type, IPAddress externalIP, string externalMAC,
        string logicalIP, string logicalPort = "")
    {
        var natRule = new PlannedNATRule(routerName)
        {
            Type = type,
            ExternalIP = externalIP.ToString(),
            ExternalMAC = externalMAC,
            LogicalIP = logicalIP,
            LogicalPort = logicalPort,
            ExternalIds = new Dictionary<string, string>
            {
                { "network_plan", plan.Id },
                { "router_name", routerName }
            }.ToMap()
        };

        return plan with { PlannedNATRules = plan.PlannedNATRules.Add(natRule.Name, natRule) };
    }

    public static NetworkPlan AddStaticRoute(this NetworkPlan plan, string routerName, string ipPrefix, IPAddress nextHop)
    {
        var route = new PlannedRouterStaticRoute(routerName)
        {
            IpPrefix = ipPrefix,
            NextHop = nextHop.ToString(),
            ExternalIds = new Dictionary<string, string>
            {
                { "network_plan", plan.Id },
                { "router_name", routerName }
            }.ToMap()
        };
        
        return plan with { PlannedRouterStaticRoutes = plan.PlannedRouterStaticRoutes.Add(route.Name, route) };
    }

    public static NetworkPlan AddRouterPort(this NetworkPlan plan, string switchName, 
        string routerName, string macAddress, IPAddress ipAddress, IPNetwork network,
        string chassisGroup = "")
    {
        var portNameRouter = $"RS-{routerName}-{switchName}";
        var portNameSwitch = $"SR-{switchName}-{routerName}";

        var routerExternalIds = new Dictionary<string, string> { { "network_plan", plan.Id } }.ToMap();
        if (!string.IsNullOrWhiteSpace(chassisGroup))
            routerExternalIds = routerExternalIds.Add("chassis_group", chassisGroup);
        
        return plan with
        {
            PlannedRouterPorts = plan.PlannedRouterPorts.Add(portNameRouter, new PlannedRouterPort(routerName)
            {
                Name = portNameRouter,
                MacAddress = macAddress,
                Networks = new Seq<string>(new[] { $"{ipAddress}/{network.Cidr}" }),
                ExternalIds = routerExternalIds
            }),
            PlannedSwitchPorts = plan.PlannedSwitchPorts.Add(portNameSwitch, new PlannedSwitchPort(switchName)
            {
                Name = portNameSwitch,
                Type = "router",
                Addresses = new Seq<string>(new[] { "router" }),
                Options = new Map<string, string>(new[] { ("router-port", portNameRouter) }),
                ExternalIds = new Dictionary<string, string> { { "network_plan", plan.Id } }.ToMap()
            })
        };
    }
}