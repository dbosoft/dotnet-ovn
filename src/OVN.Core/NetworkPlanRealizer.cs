using System.Diagnostics;
using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public class NetworkPlanRealizer
{
    private readonly ILogger _logger;
    private readonly IOVSDBTool _ovnDBTool;

    public NetworkPlanRealizer(IOVSDBTool ovnDBTool, ILogger logger)
    {
        _ovnDBTool = ovnDBTool;
        _logger = logger;
    }


    public EitherAsync<Error, NetworkPlan> ApplyNetworkPlan(
        NetworkPlan networkPlan, CancellationToken cancellationToken = default)
    {
        EitherAsync<Error, HashMap<Guid, T>> FindRecordsOfNetworkPlan<T>(
            string tableName,
            IDictionary<string, OVSFieldMetadata> columns,
            Map<Guid, Map<string, IOVSField>> additionalFields = default,
            bool global = false
        ) where T : OVSTableRecord, new()
        {
            return _ovnDBTool.FindRecords<T>(tableName,
                global 
                    ? default 
                    : CommonQueries.ExternalId("network_plan", networkPlan.Id),
                columns.Keys,
                additionalFields,
                cancellationToken).Map(s =>
            {
                var res = s
                    .Where(x => x.Id != Guid.Empty)
                    .Select(x => (x.Id, x))
                    .ToHashMap();

                return res;
            });
        }

        EitherAsync<Error, HashMap<string, TEntity>> RemoveEntitiesNotPlanned<TEntity, TPlanned>(
            string tableName,
            HashMap<Guid, TEntity> realized,
            HashMap<string, TPlanned> planned)
            where TEntity : OVSTableRecord, IOVSEntityWithName
            where TPlanned : OVSEntity
        {
            //realized is still hashed with Id column catch duplicates here
            //found will be rehashed to name
            var foundByName = realized
                .Filter(x => x.Name != null && planned.ContainsKey(x.Name))
                .Values.Select(v => (v.Name ?? "", v))
                .ToHashMap();

            //and now rehash it back to Id, to find also duplicates
            var foundById = foundByName
                .Values.Select(v => (v.Id, v))
                .ToHashMap();

            var notFound = realized - foundById;

            return notFound.Values.Map(r =>
            {
                OVSParentReference? reference = default;
                if (r is IHasParentReference hasReference) reference = hasReference.GetParentReference();

                if (!reference.HasValue)
                    return _ovnDBTool.RemoveRecord(tableName, r.Id.ToString("D"), cancellationToken)
                        .MapLeft(l =>
                            Error.New(
                                $"Apply network plan {networkPlan.Id}: could not remove entity {r} from table {tableName}",
                                l));

                if (reference.Value.RowId == Guid.Empty.ToString("D"))
                    //special case - parent not found in netplan. To remove record query for parent for this record
                    //without netplan
                    return _ovnDBTool.FindRecords<OVSTableRecord>(
                            reference.Value.TableName, new Map<string, OVSQuery>(new[]
                            {
                                (reference.Value.RefColumn, new OVSQuery(">=", new OVSValue<Guid>(r.Id)))
                            }), cancellationToken: cancellationToken)
                        .Bind(s => s.HeadOrLeft(Errors.SequenceEmpty).ToAsync())
                        .Bind(parent => _ovnDBTool.RemoveColumnValue(
                            reference.Value.TableName,
                            parent.Id.ToString("D"),
                            reference.Value.RefColumn,
                            r.Id.ToString("D"),
                            cancellationToken
                        ))
                        .MapLeft(e => Error.New(
                            $"Apply network plan {networkPlan.Id}: could not remove entity reference {r} from table {tableName}.",
                            e));

                return _ovnDBTool.RemoveColumnValue(
                    reference.Value.TableName,
                    reference.Value.RowId,
                    reference.Value.RefColumn,
                    r.Id.ToString("D"),
                    cancellationToken
                ).MapLeft(e => Error.New(
                    $"Apply network plan {networkPlan.Id}: could not remove entity reference {r} from table {reference.Value.TableName}.",
                    e));
            }).SequenceSerial().Map(_ => foundByName);
        }

        EitherAsync<Error, HashMap<string, TPlanned>> CreatePlannedEntities<TEntity, TPlanned>(
            string tableName,
            HashMap<string, TEntity> realized,
            HashMap<string, TPlanned> planned)
            where TEntity : OVSTableRecord
            where TPlanned : OVSEntity, IOVSEntityWithName
        {
            var found = planned.Filter(p => p.Name != null && realized.ContainsKey(p.Name));
            var notFound = planned - found;

            return notFound.Values.OrderBy(v => v.Name).ToSeq().Map(p =>
            {
                OVSParentReference? reference = null;
                if (p is IHasParentReference hasReference) reference = hasReference.GetParentReference();

                return _ovnDBTool.CreateRecord(tableName,
                        p.ToMap(),
                        reference,
                        cancellationToken)
                    .MapLeft(l =>
                        Error.New(
                            $"Apply network plan {networkPlan.Id}: could not create entity {p} in table {tableName}",
                            l));
            }).SequenceSerial().Map(_ => found);
        }


        EitherAsync<Error, Unit> UpdateEntities<TEntity, TPlanned>(
            string tableName,
            HashMap<string, TEntity> realized, HashMap<string, TPlanned> planned)
            where TEntity : OVSTableRecord
            where TPlanned : OVSEntity
        {
            var updates = new Map<Guid, (
                Map<string, IOVSField> AddValues,
                Map<string, IOVSField> SetValues,
                Seq<string> ClearValues)>();

            updates = planned.Map(kv =>
            {
                var plannedEntity = kv.Value;
                var plannedName = kv.Key;

                var realizedEntity = realized[plannedName];

                var plannedFields = plannedEntity.ToMap();
                var realizedFields = realizedEntity.ToMap();

                var set = new Dictionary<string, IOVSField>();
                var clear = new List<string>();
                var columns = OVSEntityMetadata.Get(typeof(TEntity));

                var processedFields = new List<string> { "_uuid", "__parentId" };
                foreach (var realizedField in realizedFields
                             .Where(realizedField => !realizedField.Key.StartsWith("_")))
                {
                    if (realizedField.Value is OVSReference)
                        continue;

                    if (!plannedFields.ContainsKey(realizedField.Key))
                    {
                        if (!columns[realizedField.Key].NotEmpty)
                            clear.Add(realizedField.Key);

                        processedFields.Add(realizedField.Key);
                        continue;
                    }

                    processedFields.Add(realizedField.Key);

                    var plannedValue = plannedFields[realizedField.Key];


                    if (plannedValue.Equals(realizedField.Value)) continue;

                    set.Add(realizedField.Key, plannedValue);
                }

                var add = plannedFields
                    .Where(pf => !processedFields.Contains(pf.Key)).ToDictionary(plannedField =>
                        plannedField.Key, plannedField => plannedField.Value);

                return add.Count > 0 || set.Count > 0 || clear.Count > 0
                    ? Some((realizedEntity.Id, (add.ToMap(), set.ToMap(), clear.ToSeq())))
                    : None;
            }).Somes().ToMap();

            return updates.Map(update =>
            {
                var (key, value) = update;

                return _ovnDBTool.UpdateRecord(tableName, key.ToString("D"),
                        value.AddValues, value.SetValues, value.ClearValues, cancellationToken)
                    .MapLeft(l => Error.New(
                        $"Apply network plan {networkPlan.Id}: could not update entity {key} in table {tableName}.",
                        l));
            }).SequenceSerial().Map(_ => Unit.Default);
        }

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

        Map<Guid, Map<string, IOVSField>> MapParentIds<T>(HashMap<Guid, T> entities, Func<T, Seq<Guid>> idFunc)
            where T : OVSEntity
        {
            return entities.Values.Map(entity =>
            {
                var map = entity.ToMap();
                if (!map.ContainsKey("_uuid") || map["_uuid"] is not OVSValue<Guid> idValue)
                    return Enumerable.Empty<(Guid, Map<string, IOVSField>)>();

                return idFunc(entity).Map(reference =>
                    (reference,
                        toMap(new[]
                        {
                            ("__parentId", (IOVSField)OVSValue<Guid>.New(idValue.Value))
                        })));
            }).Flatten().ToMap();
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
            from existingDnsRecords in FindRecordsOfNetworkPlan<DnsRecords>(OVNTableNames.DnsRecords,
                DnsRecords.Columns)
            from dnsRecords in RemoveEntitiesNotPlanned(OVNTableNames.DnsRecords, existingDnsRecords,
                networkPlan.PlannedDnsRecords)
            from existingDHCPOptions in FindRecordsOfNetworkPlan<DHCPOptions>(OVNTableNames.DHCPOptions,
                DHCPOptions.Columns)
            from dhcpOptions in RemoveEntitiesNotPlanned(OVNTableNames.DHCPOptions, existingDHCPOptions,
                networkPlan.PlannedDHCPOptions)
            
            from existingSwitches in FindRecordsOfNetworkPlan<LogicalSwitch>(OVNTableNames.LogicalSwitch,
                LogicalSwitch.Columns)
            from switches in RemoveEntitiesNotPlanned(OVNTableNames.LogicalSwitch, existingSwitches,
                networkPlan.PlannedSwitches)
            from existingRouters in FindRecordsOfNetworkPlan<LogicalRouter>(OVNTableNames.LogicalRouter,
                LogicalRouter.Columns)
            from routers in RemoveEntitiesNotPlanned(OVNTableNames.LogicalRouter, existingRouters,
                networkPlan.PlannedRouters)
            from existingSwitchPorts in FindRecordsOfNetworkPlan<LogicalSwitchPort>(OVNTableNames.LogicalSwitchPort,
                LogicalSwitchPort.Columns,
                MapParentIds(existingSwitches, s => s.Ports))
            from switchPorts in RemoveEntitiesNotPlanned(OVNTableNames.LogicalSwitchPort, existingSwitchPorts,
                networkPlan.PlannedSwitchPorts)
            from existingRouterPorts in FindRecordsOfNetworkPlan<LogicalRouterPort>(OVNTableNames.LogicalRouterPort,
                LogicalRouterPort.Columns)
            from routerPorts in RemoveEntitiesNotPlanned(OVNTableNames.LogicalRouterPort, existingRouterPorts,
                networkPlan.PlannedRouterPorts)
            from existingStaticRoutes in FindRecordsOfNetworkPlan<LogicalRouterStaticRoute>(
                OVNTableNames.LogicalRouterStaticRoutes, LogicalRouterStaticRoute.Columns,
                MapParentIds(existingRouters, s => s.StaticRoutes))
            from staticRoutes in RemoveEntitiesNotPlanned(OVNTableNames.LogicalRouterStaticRoutes, existingStaticRoutes,
                networkPlan.PlannedRouterStaticRoutes)
            from existingNATRules in FindRecordsOfNetworkPlan<NATRule>(OVNTableNames.NATRules, NATRule.Columns,
                MapParentIds(existingRouters, s => s.NATRules))
            from natRules in RemoveEntitiesNotPlanned(OVNTableNames.NATRules, existingNATRules,
                networkPlan.PlannedNATRules)
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
            from unchangedDnsRecords in CreatePlannedEntities(OVNTableNames.DnsRecords, realizedEntities.DnsRecords,
                networkPlan.PlannedDnsRecords)
            //refresh DNS options and merge then into planned switches
            from existingDnsRecords in FindRecordsOfNetworkPlan<DnsRecords>(OVNTableNames.DnsRecords,
                OVSTableRecord.Columns)
            from mappedSwitches in MapSwitchReferences(networkPlan.PlannedSwitches, existingDnsRecords)

            from unchangedDHCPOptions in CreatePlannedEntities(OVNTableNames.DHCPOptions, realizedEntities.DHCPOptions,
                networkPlan.PlannedDHCPOptions)
            //refresh DHCP options and merge then into planned ports
            from existingDHCPOptions in FindRecordsOfNetworkPlan<DHCPOptions>(OVNTableNames.DHCPOptions,
                OVSTableRecord.Columns)
            
            from mappedSwitchPorts in MapSwitchPortReferences(networkPlan.PlannedSwitchPorts, existingDHCPOptions)
            from unchangedSwitches in CreatePlannedEntities(OVNTableNames.LogicalSwitch, realizedEntities.Switches,
                mappedSwitches)
            from unchangedRouters in CreatePlannedEntities(OVNTableNames.LogicalRouter, realizedEntities.Routers,
                networkPlan.PlannedRouters)
            from unchangedSwitchPorts in CreatePlannedEntities(OVNTableNames.LogicalSwitchPort,
                realizedEntities.SwitchPorts, mappedSwitchPorts)
            
            from existingChassisGroups in FindRecordsOfNetworkPlan<ChassisGroup>(OVNTableNames.ChassisGroups,
                ChassisGroup.Columns, global: true)
            from mappedRouterPorts in MapRouterPortReferences(networkPlan.PlannedRouterPorts, existingChassisGroups)
            from unchangedRouterPorts in CreatePlannedEntities(OVNTableNames.LogicalRouterPort,
                realizedEntities.RouterPorts, mappedRouterPorts)
            from unchangedRouterStaticRoutes in CreatePlannedEntities(OVNTableNames.LogicalRouterStaticRoutes,
                realizedEntities.RouterStaticRoutes, networkPlan.PlannedRouterStaticRoutes)
            from unchangedNATRules in CreatePlannedEntities(OVNTableNames.NATRules, realizedEntities.NATRules,
                networkPlan.PlannedNATRules)
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
            from uDnsRecords in UpdateEntities(OVNTableNames.DnsRecords, entities.Realized.DnsRecords,
                entities.Planned.DnsRecords)
            from uDHCPOptions in UpdateEntities(OVNTableNames.DHCPOptions, entities.Realized.DHCPOptions,
                entities.Planned.DHCPOptions)
            from uSwitches in UpdateEntities(OVNTableNames.LogicalSwitch, entities.Realized.Switches,
                entities.Planned.Switches)
            from uRouters in UpdateEntities(OVNTableNames.LogicalRouter, entities.Realized.Routers,
                entities.Planned.Routers)
            from uSwitchPorts in UpdateEntities(OVNTableNames.LogicalSwitchPort, entities.Realized.SwitchPorts,
                entities.Planned.SwitchPorts)
            from uRouterPorts in UpdateEntities(OVNTableNames.LogicalRouterPort, entities.Realized.RouterPorts,
                entities.Planned.RouterPorts)
            from uRouterStaticRoutes in UpdateEntities(OVNTableNames.LogicalRouterStaticRoutes,
                entities.Realized.RouterStaticRoutes, entities.Planned.RouterStaticRoutes)
            from uNATRules in UpdateEntities(OVNTableNames.NATRules, entities.Realized.NATRules,
                entities.Planned.NATRules)
            from _ in StopStopWatch(sw, () => _logger.LogTrace(
                "networkPlan {networkPLanId}: Time to apply: {time} ms", networkPlan.Id, sw.ElapsedMilliseconds))
            select networkPlan;
    }
}