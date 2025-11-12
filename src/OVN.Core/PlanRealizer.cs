using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public abstract class PlanRealizer
{
    private readonly ILogger _logger;
    private readonly IOVSDBTool _ovnDBTool;

    protected PlanRealizer(IOVSDBTool ovnDBTool, ILogger logger)
    {
        _ovnDBTool = ovnDBTool;
        _logger = logger;
    }

    protected EitherAsync<Error, HashMap<Guid, T>> FindRecords<T>(
        string tableName,
        IDictionary<string, OVSFieldMetadata> columns,
        Map<string, OVSQuery> queries = default,
        CancellationToken cancellationToken = default)
        where T : OVSTableRecord, new() =>
        from records in _ovnDBTool.FindRecords<T>(
            tableName,
            queries,
            columns.Keys.ToSeq(),
            cancellationToken)
        let result = records
            .Filter(x => x.Id != Guid.Empty)
            .Map(x => (x.Id, x))
            .ToHashMap()
        select result;

    protected EitherAsync<Error, HashMap<Guid, TRecord>> FindRecordsWithParents<TRecord, TParent>(
        string tableName,
        Seq<TParent> parents,
        IDictionary<string, OVSFieldMetadata> columns,
        Map<string, OVSQuery> queries = default,
        CancellationToken cancellationToken = default)
        where TRecord : OVSTableRecord, IHasParentReference, new()
        where TParent : OVSTableRecord, IHasOVSReferences<TRecord> =>
        from records in FindRecords<TRecord>(tableName, columns, queries, cancellationToken)
        let result = records.Values.ToSeq()
            .AddParentReferences(parents)
            .Select(r => (r.Id, r))
            .ToHashMap()
        select result;

    protected EitherAsync<Error, HashMap<string, TEntity>> RemoveEntitiesNotPlanned<TEntity, TPlanned>(
        string tableName,
        HashMap<Guid, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken = default)
        where TEntity : OVSTableRecord, IOVSEntityWithName
        where TPlanned : OVSEntity =>
        from _1 in RightAsync<Error, Unit>(unit)
        //realized is still hashed with Id column catch duplicates here
        //found will be rehashed to name
        let foundByName = realized.Values
            .Filter(x => x.Name != null && planned.ContainsKey(x.Name))
            .Map(v => (v.Name ?? "", v))
            .ToHashMap()
        //and now rehash it back to Id, to find also duplicates
        let foundById = foundByName.Values
            .Map(v => (v.Id, v))
            .ToHashMap()
        let notFound = realized - foundById
        from _2 in notFound.Values
            .Map(r => RemoveEntity(tableName, r, cancellationToken)
                .MapLeft(e => Error.New($"Could not remove entity {r} from table {tableName}", e)))
            .SequenceSerial()
        select foundByName;

    private EitherAsync<Error, Unit> RemoveEntity<TEntity>(
        string tableName,
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord, IOVSEntityWithName =>
        from _1 in RightAsync<Error, Unit>(unit)
        let parentReference = entity is IHasParentReference hasReference
            ? Some(hasReference.GetParentReference())
            : None
        from _2 in parentReference.Match(
            Some: pr =>
                from resolvedParentId in ResolveParentId(pr, entity.Id, cancellationToken)
                from _ in _ovnDBTool.RemoveColumnValue(
                        pr.TableName,
                        resolvedParentId.ToString("D"),
                        pr.RefColumn,
                        entity.Id.ToString("D"),
                        cancellationToken)
                    .MapLeft(e => Error.New(
                        $"Could not remove entity reference {entity} from table {pr.TableName}.",
                        e))
                select unit,
            None: () => _ovnDBTool.RemoveRecord(tableName, entity.Id.ToString("D"), cancellationToken))
        select unit;

    private EitherAsync<Error, Guid> ResolveParentId(
        OVSParentReference parentReference,
        Guid entityId,
        CancellationToken cancellationToken) =>
        parentReference.RowId.Match(
            Some: rId =>
                from parentId in parseGuid(rId)
                    .ToEitherAsync(Error.New($"The parent ID {parentReference.RowId} is invalid."))
                select parentId,
            None: () =>
                from records in _ovnDBTool.FindRecords<OVSTableRecord>(
                        parentReference.TableName,
                        Map((parentReference.RefColumn, new OVSQuery("includes", new OVSSet<Guid>(Seq1(entityId))))),
                        cancellationToken: cancellationToken)
                    .MapLeft(e => Error.New(
                        $"Failed to search the parent of entity {entityId} in table {parentReference.TableName}.",
                        e))
                from parent in records.HeadOrNone().ToEitherAsync(
                    Error.New($"Could not find the parent of entity {entityId} in table {parentReference.TableName}."))
                select parent.Id);

    protected EitherAsync<Error, HashMap<string, TPlanned>> CreatePlannedEntities<TEntity, TPlanned>(
        string tableName,
        HashMap<string, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity, IOVSEntityWithName =>
        from _1 in RightAsync<Error, Unit>(unit)
        let found = planned.Filter(p => p.Name != null && realized.ContainsKey(p.Name))
        let notFound = planned - found
        from _ in notFound.Values
            .OrderBy(p => p.Name)
            .Map(p => CreatePlannedEntity(tableName, p, cancellationToken))
            .SequenceSerial()
        select found;

    private EitherAsync<Error, Unit> CreatePlannedEntity<TPlanned>(
        string tableName,
        TPlanned plannedEntity,
        CancellationToken cancellationToken)
        where TPlanned : OVSEntity, IOVSEntityWithName =>
        from _1 in RightAsync<Error, Unit>(unit)
        let reference = plannedEntity is IHasParentReference hasReference
            ? Some(hasReference.GetParentReference())
            : None
        from _2 in _ovnDBTool.CreateRecord(
            tableName,
            plannedEntity.ToMap(),
            reference,
            cancellationToken)
            .MapLeft(l => Error.New($"Could not create entity {plannedEntity} in table {tableName}.", l))
        select unit;

    protected EitherAsync<Error, Unit> UpdateEntities<TEntity, TPlanned>(
        string tableName,
        HashMap<string, TEntity> realized,
        HashMap<string, TPlanned> planned,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity
    {
        var updates = planned.Map(kv =>
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
                    // When field the field cannot be empty, we just skip it when
                    // it should be cleared.
                    // TODO Should we throw an error instead? Just keeping values around seems
                    // to contradict the desired state of the plan.
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
                ? Some((realizedEntity.Id, (AddValues: add.ToMap(), SetValues: set.ToMap(), ClearValues: clear.ToSeq())))
                : None;
        }).Somes().ToMap();

        return updates.Map(update =>
        {
            var (key, value) = update;

            return _ovnDBTool.UpdateRecord(tableName, key.ToString("D"),
                    update.Value.AddValues, value.SetValues, value.ClearValues, cancellationToken)
                .MapLeft(l => Error.New(
                    $"Could not update entity {key} in table {tableName}.",
                    l));
        }).SequenceSerial<Error, Unit>().Map(_ => Unit.Default);
    }
}
