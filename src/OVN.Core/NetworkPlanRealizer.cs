using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using Dbosoft.OVN.Nodes;
using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class NetworkPlanRealizer : PlanRealizer
{
    private readonly ILogger _logger;

    public NetworkPlanRealizer(IOVSDBTool ovnDBTool, ILogger logger) : base(ovnDBTool, logger)
    {
        _logger = logger;
    }

    public EitherAsync<Error, NetworkPlan> ApplyNetworkPlan(
        NetworkPlan networkPlan, CancellationToken cancellationToken = default)
    {

        EitherAsync<Error, Stopwatch> StartStopWatch()
        {
            var sw = new Stopwatch();
            sw.Start();
            return sw;
        }

        EitherAsync<Error, Unit> StopStopWatch(Stopwatch sw, Action callback)
        {
            sw.Stop();
            callback();
            return Unit.Default;
        }

        EitherAsync<Error, HashMap<string, PlannedSwitchPort>> MapSwitchPortReferences(
            HashMap<string, PlannedSwitchPort> plannedSwitchPorts, HashMap<Guid, DHCPOptions> dhcpOptions)
        {
            return plannedSwitchPorts.Map(kv =>
            {
                if (!kv.Value.ExternalIds.ContainsKey("dhcp_options_v4"))
                    return kv;

                var optionId = kv.Value.ExternalIds["dhcp_options_v4"];

                if (string.IsNullOrWhiteSpace(optionId)) return kv;

                var port = dhcpOptions.Find(x =>
                        x.Value.Name == optionId)
                    .Match(
                        None: () => kv.Value,
                        Some: option =>
                        {
                            return kv.Value with { DHCPOptionsRefV4 = new Seq<Guid>(new[] { option.Value.Id }) };
                        });

                return (kv.Key, port);
            }).ToHashMap();
        }
        
        EitherAsync<Error, HashMap<string, PlannedRouterPort>> MapRouterPortReferences(
            HashMap<string, PlannedRouterPort> plannedSwitchPorts, HashMap<Guid, ChassisGroup> chassisGroups)
        {
            return plannedSwitchPorts.Map(kv =>
            {
                if (!kv.Value.ExternalIds.ContainsKey("chassis_group"))
                    return kv;

                var chassisGroup = kv.Value.ExternalIds["chassis_group"];

                if (string.IsNullOrWhiteSpace(chassisGroup)) return kv;

                var port = chassisGroups.Find(x =>
                        x.Value.Name == chassisGroup)
                    .Match(
                        None: () => kv.Value,
                        Some: option => kv.Value with { ChassisGroupRef  = 
                            new Seq<Guid>(new [] {option.Value.Id })});

                return (kv.Key, port);
            }).ToHashMap();
        }
        
        EitherAsync<Error, HashMap<string, PlannedSwitch>> MapSwitchReferences(
            HashMap<string, PlannedSwitch> plannedSwitchPorts, HashMap<Guid, DnsRecords> dnsRecords)
        {
            return plannedSwitchPorts.Map(kv =>
            {
                if (!kv.Value.ExternalIds.ContainsKey("dns_records"))
                    return kv;

                var recordsIds = kv.Value.ExternalIds["dns_records"];

                if (string.IsNullOrWhiteSpace(recordsIds)) return kv;

                var records = recordsIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                
                var ports =
                    records.Map(recordId => dnsRecords.Values.Find(x =>
                        x.Name == recordId).Select(x=>x.Id).ToSeq()).ToSeq().Flatten();

                return (kv.Key, kv.Value with { DnsRecords = new Seq<Guid>( ports)});
            }).ToHashMap();
        }


        var getAndCleanupUnplannedEntities =
            from existingDnsRecords in FindRecords<DnsRecords>(
                networkPlan.Id,
                OVNTableNames.DnsRecords,
                DnsRecords.Columns,
                cancellationToken)
            from dnsRecords in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.DnsRecords,
                existingDnsRecords,
                networkPlan.PlannedDnsRecords,
                cancellationToken)
            from existingDHCPOptions in FindRecords<DHCPOptions>(
                networkPlan.Id,
                OVNTableNames.DHCPOptions,
                DHCPOptions.Columns,
                cancellationToken)
            from dhcpOptions in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.DHCPOptions,
                existingDHCPOptions,
                networkPlan.PlannedDHCPOptions,
                cancellationToken)
            
            from existingSwitches in FindRecords<LogicalSwitch>(
                networkPlan.Id,
                OVNTableNames.LogicalSwitch,
                LogicalSwitch.Columns,
                cancellationToken)
            from switches in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.LogicalSwitch,
                existingSwitches,
                networkPlan.PlannedSwitches,
                cancellationToken)
            from existingRouters in FindRecords<LogicalRouter>(
                networkPlan.Id,
                OVNTableNames.LogicalRouter,
                LogicalRouter.Columns,
                cancellationToken)
            from routers in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.LogicalRouter,
                existingRouters,
                networkPlan.PlannedRouters,
                cancellationToken)
            from existingSwitchPorts in FindRecordsWithParents<LogicalSwitchPort, LogicalSwitch>(
                networkPlan.Id,
                OVNTableNames.LogicalSwitchPort,
                existingSwitches.Values.ToSeq(),
                LogicalSwitchPort.Columns,
                cancellationToken)
            from switchPorts in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.LogicalSwitchPort,
                existingSwitchPorts,
                networkPlan.PlannedSwitchPorts,
                cancellationToken)
            from existingRouterPorts in FindRecordsWithParents<LogicalRouterPort, LogicalRouter>(
                networkPlan.Id,
                OVNTableNames.LogicalRouterPort,
                routers.Values.ToSeq(),
                LogicalRouterPort.Columns,
                cancellationToken)
            from routerPorts in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.LogicalRouterPort,
                existingRouterPorts,
                networkPlan.PlannedRouterPorts,
                cancellationToken)
            from existingStaticRoutes in FindRecordsWithParents<LogicalRouterStaticRoute, LogicalRouter>(
                networkPlan.Id,
                OVNTableNames.LogicalRouterStaticRoutes,
                existingRouters.Values.ToSeq(),
                LogicalRouterStaticRoute.Columns,
                cancellationToken)
            from staticRoutes in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.LogicalRouterStaticRoutes,
                existingStaticRoutes,
                networkPlan.PlannedRouterStaticRoutes,
                cancellationToken)
            from existingNATRules in FindRecordsWithParents<NATRule, LogicalRouter>(
                networkPlan.Id,
                OVNTableNames.NATRules,
                existingRouters.Values.ToSeq(),
                NATRule.Columns,
                cancellationToken)
            from natRules in RemoveEntitiesNotPlanned(
                networkPlan.Id,
                OVNTableNames.NATRules,
                existingNATRules,
                networkPlan.PlannedNATRules,
                cancellationToken)
            select (
                DnsRecords: dnsRecords,
                DHCPOptions: dhcpOptions,
                Switches: switches,
                Routers: routers,
                SwitchPorts: switchPorts,
                RouterPorts: routerPorts,
                RouterStaticRoutes: staticRoutes,
                NATRules: natRules);

        var createAndGetUnchangedEntities = from realizedEntities in getAndCleanupUnplannedEntities
            from unchangedDnsRecords in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.DnsRecords,
                realizedEntities.DnsRecords,
                networkPlan.PlannedDnsRecords,
                cancellationToken)
            //refresh DNS options and merge then into planned switches
            from existingDnsRecords in FindRecords<DnsRecords>(
                networkPlan.Id,
                OVNTableNames.DnsRecords,
                OVSTableRecord.Columns,
                cancellationToken)
            from mappedSwitches in MapSwitchReferences(networkPlan.PlannedSwitches, existingDnsRecords)

            from unchangedDHCPOptions in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.DHCPOptions,
                realizedEntities.DHCPOptions,
                networkPlan.PlannedDHCPOptions,
                cancellationToken)
            //refresh DHCP options and merge then into planned ports
            from existingDHCPOptions in FindRecords<DHCPOptions>(
                networkPlan.Id,
                OVNTableNames.DHCPOptions,
                OVSTableRecord.Columns,
                cancellationToken)
            
            from mappedSwitchPorts in MapSwitchPortReferences(networkPlan.PlannedSwitchPorts, existingDHCPOptions)
            from unchangedSwitches in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.LogicalSwitch,
                realizedEntities.Switches,
                mappedSwitches,
                cancellationToken)
            from unchangedRouters in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouter,
                realizedEntities.Routers,
                networkPlan.PlannedRouters,
                cancellationToken)
            from unchangedSwitchPorts in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.LogicalSwitchPort,
                realizedEntities.SwitchPorts,
                mappedSwitchPorts,
                cancellationToken)
            
            from existingChassisGroups in FindRecords<ChassisGroup>(
                OVNTableNames.ChassisGroups,
                ChassisGroup.Columns,
                cancellationToken: cancellationToken)
            from mappedRouterPorts in MapRouterPortReferences(networkPlan.PlannedRouterPorts, existingChassisGroups)
            from unchangedRouterPorts in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouterPort,
                realizedEntities.RouterPorts,
                mappedRouterPorts,
                cancellationToken)
            from unchangedRouterStaticRoutes in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouterStaticRoutes,
                realizedEntities.RouterStaticRoutes,
                networkPlan.PlannedRouterStaticRoutes,
                cancellationToken)
            from unchangedNATRules in CreatePlannedEntities(
                networkPlan.Id,
                OVNTableNames.NATRules,
                realizedEntities.NATRules,
                networkPlan.PlannedNATRules,
                cancellationToken)
            select (Realized: realizedEntities,
                    Planned: (
                        DnsRecords: unchangedDnsRecords,
                        DHCPOptions: unchangedDHCPOptions,
                        Switches: unchangedSwitches,
                        Routers: unchangedRouters,
                        SwitchPorts: unchangedSwitchPorts,
                        RouterPorts: unchangedRouterPorts,
                        RouterStaticRoutes: unchangedRouterStaticRoutes,
                        NATRules: unchangedNATRules)
                );

        return
            from sw in StartStopWatch()
            from entities in createAndGetUnchangedEntities
            from uDnsRecords in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.DnsRecords,
                entities.Realized.DnsRecords,
                entities.Planned.DnsRecords,
                cancellationToken)
            from uDHCPOptions in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.DHCPOptions,
                entities.Realized.DHCPOptions,
                entities.Planned.DHCPOptions,
                cancellationToken)
            from uSwitches in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.LogicalSwitch,
                entities.Realized.Switches,
                entities.Planned.Switches,
                cancellationToken)
            from uRouters in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouter,
                entities.Realized.Routers,
                entities.Planned.Routers,
                cancellationToken)
            from uSwitchPorts in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.LogicalSwitchPort,
                entities.Realized.SwitchPorts,
                entities.Planned.SwitchPorts,
                cancellationToken)
            from uRouterPorts in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouterPort,
                entities.Realized.RouterPorts,
                entities.Planned.RouterPorts,
                cancellationToken)
            from uRouterStaticRoutes in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.LogicalRouterStaticRoutes,
                entities.Realized.RouterStaticRoutes,
                entities.Planned.RouterStaticRoutes,
                cancellationToken)
            from uNATRules in UpdateEntities(
                networkPlan.Id,
                OVNTableNames.NATRules,
                entities.Realized.NATRules,
                entities.Planned.NATRules,
                cancellationToken)
            from _ in StopStopWatch(sw, () => _logger.LogTrace(
                "networkPlan {networkPLanId}: Time to apply: {time} ms", networkPlan.Id, sw.ElapsedMilliseconds))
            select networkPlan;
    }

    EitherAsync<Error, HashMap<Guid, T>> FindRecords<T>(
        string networkPlanId,
        string tableName,
        IDictionary<string, OVSFieldMetadata> columns,
        CancellationToken cancellationToken = default)
        where T : OVSTableRecord, new() =>
        FindRecords<T>(
            tableName,
            columns,
            CommonQueries.ExternalId("network_plan", networkPlanId),
            cancellationToken);


    EitherAsync<Error, HashMap<Guid, TRecord>> FindRecordsWithParents<TRecord, TParent>(
        string networkPlanId,
        string tableName,
        Seq<TParent> parents,
        IDictionary<string, OVSFieldMetadata> columns,
        CancellationToken cancellationToken = default)
        where TRecord : OVSTableRecord, IHasParentReference, new()
        where TParent : OVSTableRecord, IHasOVSReferences<TRecord> =>
        FindRecordsWithParents<TRecord, TParent>(
            tableName,
            parents,
            columns,
            CommonQueries.ExternalId("network_plan", networkPlanId),
            cancellationToken);

    private EitherAsync<Error, HashMap<string, TEntity>> RemoveEntitiesNotPlanned<TEntity, TPlanned>(
        string networkPlanId,
        string tableName,
        HashMap<Guid, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord, IOVSEntityWithName
        where TPlanned : OVSEntity =>
        RemoveEntitiesNotPlanned(tableName, realized, planned, cancellationToken)
            .MapLeft(e => Error.New($"Cannot apply network plan {networkPlanId}.", e));

    EitherAsync<Error, HashMap<string, TPlanned>> CreatePlannedEntities<TEntity, TPlanned>(
        string networkPlanId,
        string tableName,
        HashMap<string, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity, IOVSEntityWithName =>
        CreatePlannedEntities(tableName, realized, planned, cancellationToken)
            .MapLeft(e => Error.New($"Cannot apply network plan {networkPlanId}.", e));

    EitherAsync<Error, Unit> UpdateEntities<TEntity, TPlanned>(
        string networkPlanId,
        string tableName,
        HashMap<string, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity =>
        UpdateEntities(tableName, realized, planned, cancellationToken)
            .MapLeft(e => Error.New($"Cannot apply network plan {networkPlanId}.", e));
}
