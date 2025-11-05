using System.Net;
using System.Net.Sockets;
using Dbosoft.OVN;
using LanguageExt;

namespace Dbosoft.OVNAgent;

public static class NetworkPlanParser
{
    
    public static NetworkPlan ParseYaml(IDictionary<object, object> dictionary)
    {
        if (!dictionary.ContainsKey("id") || dictionary["id"] is not string id)
            throw new InvalidDataException("network plan id is required.");

        var plan = new NetworkPlan(id);

        if (dictionary.ContainsKey("switches")
            && dictionary["switches"] is IList<object> switches)
        {
            plan = ParseSwitches(plan, switches);
        }
        
        if (dictionary.ContainsKey("routers")
            && dictionary["routers"] is IList<object> routers)
        {
            plan = ParseRouters(plan, routers);
        }
        
        if (dictionary.ContainsKey("dhcp_options_v4")
            && dictionary["dhcp_options_v4"] is IList<object> dhcpOptions)
        {
            plan = ParseDHCPOptionsV4(plan, dhcpOptions);
        }

        return plan;
        
        
        NetworkPlan ParseRouters(NetworkPlan networkPlan, IEnumerable<object> routers)
        {
            foreach (var networkRoutersObj in routers)
            {
                if (networkRoutersObj is not IDictionary<object, object> networkRouter) 
                    continue;
                
                networkPlan= ParseRouter(networkPlan, networkRouter);
            }
            
            return networkPlan;
        }
        
        
        NetworkPlan ParseSwitches(NetworkPlan networkPlan, IEnumerable<object> switches)
        {
            foreach (var networkSwitchObj in switches)
            {
                if (networkSwitchObj is not IDictionary<object, object> networkSwitch) 
                    continue;
                
                networkPlan= ParseSwitch(networkPlan, networkSwitch);
            }
            
            return networkPlan;
        }
        
        NetworkPlan ParseRouter(NetworkPlan networkPlan, 
            IDictionary<object, object> routerValues)
        {
            if (!routerValues.ContainsKey("name") || routerValues["name"] is not string routerName)
                throw new InvalidDataException("router name is required.");

            networkPlan = networkPlan.AddRouter(routerName);

            if (routerValues.ContainsKey("gateway_port")
                && routerValues["gateway_port"] is Dictionary<object,object> gatewayPortValues)
            {
                networkPlan = ParseGatewayPort(networkPlan, gatewayPortValues, routerName);
            }
            
            if (routerValues.ContainsKey("routes")
                && routerValues["routes"] is IList<object> routes)
            {
                foreach (var routeObj in routes)
                {
                    if (routeObj is IDictionary<object, object> routeValues)
                        networkPlan = ParseRouterRoutes(networkPlan, routeValues, routerName);
                }
            }
            
            if (routerValues.ContainsKey("nat")
                && routerValues["nat"] is IList<object> natRules)
            {
                foreach (var natRuleObj in natRules)
                {
                    if (natRuleObj is IDictionary<object, object> natRule)
                        networkPlan = ParseRouterNAT(networkPlan, natRule, routerName);
                }
            }
            
            if (!routerValues.ContainsKey("ports") || routerValues["ports"] is not IList<object> routerPorts)
                return networkPlan;
            
            foreach (var routerPortObj in routerPorts)
            {
                if (routerPortObj is IDictionary<object, object> routerPortValues)
                    networkPlan = ParseRouterPort(networkPlan, routerPortValues, routerName);
            }


            return networkPlan;
        }

        
        NetworkPlan ParseSwitch(NetworkPlan networkPlan, 
            IDictionary<object, object> switchValues)
        {
            if (!switchValues.ContainsKey("name") || switchValues["name"] is not string switchName)
                throw new InvalidDataException("switch name is required.");

            networkPlan = networkPlan.AddSwitch(switchName);

            if (!switchValues.ContainsKey("ports") || switchValues["ports"] is not IList<object> switchPorts)
                return networkPlan;
            
            foreach (var switchPortObj in switchPorts)
            {
                if (switchPortObj is IDictionary<object, object> switchPortValues)
                    networkPlan = ParseSwitchPort(networkPlan, switchPortValues, switchName);
            }


            return networkPlan;
        }
        
        NetworkPlan ParseSwitchPort(NetworkPlan networkPlan, 
            IDictionary<object, object> portValues, string switchName)
        {
            var firstKey = portValues.Keys.FirstOrDefault();
            networkPlan = firstKey switch
            {
                "network" => ParseExternalPort(networkPlan, portValues, switchName),
                _ => ParseGenericPort(networkPlan, portValues, switchName)
            };

            return networkPlan;
        }
        
        NetworkPlan ParseGatewayPort(NetworkPlan networkPlan, 
            IDictionary<object, object> portValues, string routerName)
        {
            
            if (!portValues.ContainsKey("chassis_group")
                || portValues["chassis_group"] is not string chassisGroup)
                throw new InvalidDataException($"switch name is required for gateway port (router: {routerName})");
            
            if (!portValues.ContainsKey("switch")
                || portValues["switch"] is not string switchName)
                throw new InvalidDataException($"switch name is required for gateway port (router: {routerName})");
            
            if (!portValues.ContainsKey("mac")
                || portValues["mac"] is not string macAddress)
                throw new InvalidDataException($"mac address is required for gateway port (router: {routerName})");

            if (!portValues.ContainsKey("network")
                || portValues["network"] is not string networkString)
                throw new InvalidDataException($"network is required for gateway port (router: {routerName})");

            var networkParts = networkString.Split('/');
            if (networkParts.Length != 2
                || !IPAddress.TryParse(networkParts[0], out var ipAddress)
                || !IPNetwork2.TryParse(networkString, out var network))
            {
                throw new InvalidDataException(
                    $"network {networkString} is invalid for gateway port (router: {routerName})\n" +
                    "A valid IP address followed by network cidr is required (e.g. 192.168.2.10/24).");
            }
            
            return networkPlan.AddRouterPort(
                switchName, routerName, macAddress, ipAddress, network, chassisGroup);
        }
        
        NetworkPlan ParseRouterPort(NetworkPlan networkPlan, 
            IDictionary<object, object> portValues, string routerName)
        {

            if (!portValues.ContainsKey("switch")
                || portValues["switch"] is not string switchName)
                throw new InvalidDataException($"switch name is required for router port (router: {routerName})");
            
            if (!portValues.ContainsKey("mac")
                || portValues["mac"] is not string macAddress)
                throw new InvalidDataException($"mac address is required for router port (router: {routerName}, switch: {switchName})");

            if (!portValues.ContainsKey("network")
                || portValues["network"] is not string networkString)
                throw new InvalidDataException($"network is required for router port (router: {routerName}, switch: {switchName})");

            var networkParts = networkString.Split('/');
            if (networkParts.Length != 2
                || !IPAddress.TryParse(networkParts[0], out var ipAddress)
                || !IPNetwork2.TryParse(networkString, out var network))
            {
                throw new InvalidDataException(
                    $"network {networkString} is invalid for router port (router: {routerName}, switch: {switchName})\n" +
                    "A valid IP address followed by network cidr is required (e.g. 192.168.2.10/24).");
            }
            
            return networkPlan.AddRouterPort(
                switchName, routerName, macAddress, ipAddress, network);
        }
        
        NetworkPlan ParseRouterNAT(NetworkPlan networkPlan, 
            IDictionary<object, object> natValues, string routerName)
        {
            var natType = "";
            var externalIPString = "";
            var logicalIP = "";
            var externalMac = "";
            var logicalPort = "";
            if (natValues.ContainsKey("snat"))
            {
                natType = "snat";
                if (natValues["snat"] is IDictionary<object, object> snatValues)
                {
                    if(snatValues.ContainsKey("external_address") &&
                       snatValues["external_address"] is string externalAddressString)
                       externalIPString = externalAddressString;
                    
                    if(snatValues.ContainsKey("network") &&
                       snatValues["network"] is string addressString)
                        logicalIP = addressString;
                }
            }
            
            if (natValues.ContainsKey("dnat_and_snat") || natValues.ContainsKey("dnat") )
            {
                natType = natValues.ContainsKey("dnat_and_snat") ? "dnat_and_snat" : "dnat";
                if (natValues[natType] is IDictionary<object, object> dnatValues)
                {
                    if(dnatValues.ContainsKey("external_address") &&
                       dnatValues["external_address"] is string externalAddressString)
                        externalIPString = externalAddressString;
                    
                    if(dnatValues.ContainsKey("mac") &&
                       dnatValues["mac"] is string macString)
                        externalMac = macString;
                    
                    if(dnatValues.ContainsKey("address") &&
                       dnatValues["address"] is string addressString)
                        logicalIP = addressString;
                    
                    if(dnatValues.ContainsKey("port") &&
                       dnatValues["port"] is string portNameString)
                        logicalPort = portNameString;
                }
            }

            if (string.IsNullOrWhiteSpace(natType))
                throw new InvalidDataException($"invalid nat rule for router {routerName}. " +
                                               "Supported rules: snat, dnat, dnat_and_snat");
            
            if(!IPAddress.TryParse(externalIPString, out var ipAddress))
                throw new InvalidDataException(
                    $"ip address {externalIPString} is invalid for router nat (router: {routerName}).");
            
            return networkPlan.AddNATRule(routerName, natType, ipAddress, externalMac, logicalIP, logicalPort);
        }
        
        NetworkPlan ParseRouterRoutes(NetworkPlan networkPlan, 
            IDictionary<object, object> routeValues, string routerName)
        {

            if (!routeValues.ContainsKey("route")
                || routeValues["route"] is not string ipPrefix)
                throw new InvalidDataException($"route name is required for router route (router: {routerName})");
            
            if (!routeValues.ContainsKey("nexthop")
                || routeValues["nexthop"] is not string nextHopString)
                throw new InvalidDataException($"nextHop is required for router route (router: {routerName}, route: {ipPrefix})");
            
            if(!IPAddress.TryParse(nextHopString, out var nextHop))
                throw new InvalidDataException($"nextHop {nextHopString} is invalid for router route (router: {routerName}, route: {ipPrefix})");

            return networkPlan.AddStaticRoute(
                routerName, ipPrefix, nextHop);
        }
        
        NetworkPlan ParseExternalPort(NetworkPlan networkPlan, 
            IDictionary<object, object> portValues, string switchName)
        {
            if (!portValues.ContainsKey("network")
                || portValues["network"] is not string networkName)
                throw new InvalidDataException($"network name is required for network port (switch: {switchName})");

            int? tag = null;

            if (portValues.ContainsKey("tag")
                && portValues["tag"] is int tagValue)
            {
                tag = tagValue;
            }

            return networkPlan
                .AddExternalNetworkPort(switchName, networkName, tag);
        }
        
        NetworkPlan ParseGenericPort(NetworkPlan networkPlan, 
            IDictionary<object, object> portValues, string switchName)
        {
            if (!portValues.ContainsKey("name")
                || portValues["name"] is not string portName)
                throw new InvalidDataException($"port name is required for generic ports (switch: {switchName})");

            if (!portValues.ContainsKey("mac")
                || portValues["mac"] is not string macAddress)
                throw new InvalidDataException($"mac address is required for port (switch: {switchName}, port: {portName})");

            if (!portValues.ContainsKey("address")
                || portValues["address"] is not string addressString)
                throw new InvalidDataException($"address is required for port (switch: {switchName}, port: {portName})");

            if(!IPAddress.TryParse(addressString, out var ipAddress))
                throw new InvalidDataException($"invalid ipaddress {addressString} for port (switch: {switchName}, port: {portName})");

            var dhcpV4 = "";
            if (portValues.ContainsKey("dhcp_options_v4")
                && portValues["dhcp_options_v4"] is string dhcpV4String)
            {
                dhcpV4 = dhcpV4String;
            }

            return networkPlan.AddNetworkPort(switchName, portName, macAddress, ipAddress, dhcpV4);
        }
        
        NetworkPlan ParseDHCPOptionsV4(NetworkPlan networkPlan, IEnumerable<object> dhcpOptions)
        {
            foreach (var dhcpOptionsObj in dhcpOptions)
            {
                if (dhcpOptionsObj is not IDictionary<object, object> dhcpOption) 
                    continue;
                
                networkPlan= ParseDHCPOptionV4(networkPlan, dhcpOption);
            }
            
            return networkPlan;
        }
        
        NetworkPlan ParseDHCPOptionV4(NetworkPlan networkPlan, 
            IDictionary<object, object> optionValue)
        {
            if (!optionValue.ContainsKey("name")
                || optionValue["name"] is not string optionName)
                throw new InvalidDataException($"name is required for v4 dhcp options.");

            if (!optionValue.ContainsKey("cidr")
                || optionValue["cidr"] is not string cidrString)
                throw new InvalidDataException($"cidr is required for v4 dhcp options (options: {optionName})");

            if(!IPNetwork2.TryParse(cidrString, out var cidrNetwork) 
               || cidrNetwork.AddressFamily != AddressFamily.InterNetwork)
                throw new InvalidDataException($"invalid cidr address {cidrString} for v4 dhcp options (options: {optionName})");
        
            if (!optionValue.ContainsKey("server_id")
                || optionValue["server_id"] is not string serverIdString)
                throw new InvalidDataException($"server_id is required for v4 dhcp options (options: {optionName})");

            if(!IPAddress.TryParse(serverIdString, out var serverId) 
               || cidrNetwork.AddressFamily != AddressFamily.InterNetwork)
                throw new InvalidDataException($"invalid server_id address {serverId} for v4 dhcp options (options: {optionName})");

            if (!optionValue.ContainsKey("server_mac")
                || optionValue["server_mac"] is not string serverMAC)
                throw new InvalidDataException($"server_mac is required for v4 dhcp options (options: {optionName})");

            if (!optionValue.ContainsKey("lease_time")
                || optionValue["lease_time"] is not string leaseTimeString)
                throw new InvalidDataException($"lease_time is required for v4 dhcp options (options: {optionName})");

            var options = new Map<string, string>()
                .Add("server_id", serverId.ToString())
                .Add("server_mac", serverMAC)
                .Add("lease_time", leaseTimeString);

            if (optionValue.ContainsKey("router")
                && optionValue["router"] is string routerString)
            {
                if(!IPAddress.TryParse(routerString, out var routerIP) 
                   || routerIP.AddressFamily != AddressFamily.InterNetwork)
                    throw new InvalidDataException($"invalid router option {routerString} for v4 dhcp options (options: {optionName})");

                options = options.Add("router", routerString);
            }
            
            if (optionValue.ContainsKey("dns_servers")
                && optionValue["dns_servers"] is List<object> dnsList)
            {
                foreach (var dnsServer in dnsList.Cast<string>())
                {
                    if(!IPAddress.TryParse(dnsServer, out var routerIP) 
                       || routerIP.AddressFamily != AddressFamily.InterNetwork)
                        throw new InvalidDataException($"invalid dns_server value {dnsServer} for v4 dhcp options (options: {optionName})");
                }

                var dnsServers = dnsList.Count == 1
                    ? dnsList[0].ToString() ?? ""
                    : $"{{{string.Join(',', dnsList)}}}";

                options = options.Add("dns_server", dnsServers);
            }
            
            if (optionValue.ContainsKey("domain_name")
                && optionValue["domain_name"] is string domainName)
            {
                options = options.Add("domain_name", domainName);
            }
            
            if (optionValue.ContainsKey("mtu")
                && optionValue["mtu"] is string mtu)
            {
                options = options.Add("mtu", mtu);
            }

            return networkPlan.AddDHCPOptions(optionName, cidrNetwork, options);
        }
    }
}