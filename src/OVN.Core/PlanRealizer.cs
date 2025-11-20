using System.CodeDom;
using System.Security.Cryptography;
using System.Text;
using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

public abstract class PlanRealizer
{
    private const string PemCertificateHeader = "-----BEGIN CERTIFICATE-----";
    private const string PemPrivateKeyHeader = "-----BEGIN PRIVATE KEY-----";

    private readonly ISystemEnvironment _systemEnvironment;
    private readonly IOVSDBTool _ovnDBTool;

    protected PlanRealizer(ISystemEnvironment systemEnvironment, IOVSDBTool ovnDBTool)
    {
        _systemEnvironment = systemEnvironment;
        _ovnDBTool = ovnDBTool;
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
        // By creating the hash map by name, we also detect duplicate names.
        // Only the first entity with a given name will be kept. Hence, the
        // duplicates will later be removed from the database as they are
        // considered as not planned.
        let foundByName = realized.Values
            .Filter(x => x.Name != null && planned.ContainsKey(x.Name))
            .Map(v => (v.Name!, v))
            .ToHashMap()
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
        HashMap<string, TEntity> realizedEntities,
        HashMap<string, TPlanned> plannedEntities,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity =>
        from _ in plannedEntities.ToSeq()
            .Map(p => UpdateEntity(tableName, realizedEntities, p.Key, p.Value, cancellationToken))
            .SequenceSerial()
        select unit;

    private EitherAsync<Error, Unit> UpdateEntity<TEntity, TPlanned>(
        string tableName,
        HashMap<string, TEntity> realizedEntities,
        string plannedEntityName,
        TPlanned plannedEntity,
        CancellationToken cancellationToken)
        where TEntity : OVSTableRecord
        where TPlanned : OVSEntity =>
        from realizedEntity in realizedEntities.Find(plannedEntityName)
            .ToEitherAsync(Error.New($"The entity '{plannedEntityName}' cannot be updated as it does not exist."))
        let columns = OVSEntityMetadata.Get(typeof(TEntity))
        let plannedFields = plannedEntity.ToMap().Filter(IsUpdatable)
        let realizedFields = realizedEntity.ToMap().Filter(IsUpdatable)
        let addedFields = plannedFields.Except(realizedFields)
        let removedFields = realizedFields.Except(plannedFields)
        let updatedFields = plannedFields.Intersect(
                realizedFields,
                (name, plannedValue, realizedValue) =>
                    (Name: name, PlannedValue: plannedValue, RealizedValue: realizedValue))
            .Filter(t => t.PlannedValue != t.RealizedValue)
            .Map(t => t.PlannedValue)
        from _ in addedFields.IsEmpty && removedFields.IsEmpty && updatedFields.IsEmpty
            ? RightAsync<Error, Unit>(unit)
            : _ovnDBTool.UpdateRecord(
                    tableName,
                    realizedEntity.Id.ToString("D"),
                    addedFields,
                    updatedFields,
                    removedFields.Keys.ToSeq(),
                    cancellationToken)
                .MapLeft(e => Error.New($"Could not update entity '{realizedEntity}' in table '{tableName}'.", e))
        select unit;

    private static bool IsUpdatable(string name, IOVSField value) =>
        name is not ("_uuid" or "__parentId") && value is not OVSReference;

    /// <summary>
    /// Applies the planned SSL configuration to the database.
    /// </summary>
    /// <remarks>
    /// The SSL configuration is special as only one configuration
    /// can exist at any given time.
    /// </remarks>
    protected EitherAsync<Error, Unit> ApplySsl<TPlannedSsl, TSsl, TGlobal>(
        Option<TPlannedSsl> plannedSsl,
        string globalTableName,
        CancellationToken cancellationToken)
        where TPlannedSsl : PlannedOvsSsl
        where TSsl : OVSSslTableRecord, new()
        where TGlobal : OVSGlobalTableRecord, IHasOVSReferences<TSsl>, new() =>
        from global in FindRecords<TGlobal>(
            globalTableName,
            OVSGlobalTableRecord.Columns,
            cancellationToken: cancellationToken)
        from sslWithPaths in EnsureCertificateFiles(
            plannedSsl,
            cancellationToken)
        let plannedSslMap = sslWithPaths
            .Map(s => (s.Name, s))
            .ToHashMap()
        from existingSsl in FindRecordsWithParents<TSsl, TGlobal>(
            "SSL",
            global.Values.ToSeq(),
            OVSSslTableRecord.Columns,
            cancellationToken: cancellationToken)
        from remainingSsl in RemoveEntitiesNotPlanned<TSsl, TPlannedSsl>(
            "SSL",
            existingSsl,
            plannedSslMap,
            cancellationToken: cancellationToken)
        from existingPlannedSsl in CreatePlannedEntities<TSsl, TPlannedSsl>(
            "SSL",
            remainingSsl,
            plannedSslMap,
            cancellationToken: cancellationToken)
        from _3 in UpdateEntities(
            "SSL",
            remainingSsl,
            existingPlannedSsl,
            cancellationToken: cancellationToken)
        from _4 in RemovedUnusedCertificates(sslWithPaths, existingSsl)
        select unit;

    private EitherAsync<Error, Option<TPlannedSsl>> EnsureCertificateFiles<TPlannedSsl>(
        Option<TPlannedSsl> plannedSouthboundSsl,
        CancellationToken cancellationToken)
        where TPlannedSsl : PlannedOvsSsl =>
        from updated in plannedSouthboundSsl
            .Map(s => EnsureCertificateFiles(s, cancellationToken))
            .Sequence()
        select updated;

    private EitherAsync<Error, TPlannedSsl> EnsureCertificateFiles<TPlannedSsl>(
        TPlannedSsl plannedSouthboundSsl,
        CancellationToken cancellationToken)
        where TPlannedSsl : PlannedOvsSsl =>
        from caCertificatePem in Optional(plannedSouthboundSsl.CaCertificate)
            .ToEitherAsync(Error.New("The CA certificate is missing"))
        from _1 in guard(caCertificatePem.StartsWith(PemCertificateHeader),
            Error.New("The CA certificate must be a PEM encoded string."))
        from certificatePem in Optional(plannedSouthboundSsl.Certificate)
            .ToEitherAsync(Error.New("The certificate is missing"))
        from _2 in guard(certificatePem.StartsWith(PemCertificateHeader),
            Error.New("The certificate must be a PEM encoded string."))
        from privateKeyPem in Optional(plannedSouthboundSsl.PrivateKey)
            .ToEitherAsync(Error.New("The private key is missing"))
        from _3 in guard(privateKeyPem.StartsWith(PemPrivateKeyHeader),
            Error.New("The private key must be a PEM encoded string."))
        // The generated file names contain a hash of the content. This
        // way, the changes can be made transactionally as changes do
        // not overwrite existing files.
        let caCertificateFile = new OvsFile(
            "/etc/openvswitch",
            $"cacert_{ComputeHash(caCertificatePem)}.pem")
        let certificateFile = new OvsFile(
            "/etc/openvswitch",
            $"cert_{ComputeHash(certificatePem)}.pem")
        let privateKeyFile = new OvsFile(
            "/etc/openvswitch/private",
            $"key_{ComputeHash(privateKeyPem)}.pem")
        from _4 in EnsureCertificateFile(caCertificateFile, caCertificatePem, adminOnly: false, cancellationToken)
        from _5 in EnsureCertificateFile(certificateFile, certificatePem, adminOnly: false, cancellationToken)
        from _6 in EnsureCertificateFile(privateKeyFile, privateKeyPem, adminOnly: true, cancellationToken)
        select plannedSouthboundSsl with
        {
            CaCertificate = _systemEnvironment.FileSystem.ResolveOvsFilePath(caCertificateFile),
            Certificate = _systemEnvironment.FileSystem.ResolveOvsFilePath(certificateFile),
            PrivateKey = _systemEnvironment.FileSystem.ResolveOvsFilePath(privateKeyFile)
        };

    private EitherAsync<Error, Unit> EnsureCertificateFile(
        OvsFile file,
        string content,
        bool adminOnly,
        CancellationToken cancellationToken) =>
        from _1 in RightAsync<Error, Unit>(unit)
        let path = _systemEnvironment.FileSystem.ResolveOvsFilePath(file, false)
        from _2 in TryAsync(async () =>
        {
            if (_systemEnvironment.FileSystem.FileExists(file))
                return unit;

            _systemEnvironment.FileSystem.EnsurePathForFileExists(file, adminOnly);
            await _systemEnvironment.FileSystem.WriteFileAsync(file, content, cancellationToken);
            return unit;
        }).ToEither()
        select unit;

    private EitherAsync<Error, Unit> RemovedUnusedCertificates<TPlannedSsl, TSsl>(
        Option<TPlannedSsl> plannedSsl,
        HashMap<Guid, TSsl> existingSsl)
        where TPlannedSsl : PlannedOvsSsl
        where TSsl : OVSSslTableRecord, new() =>
        from _1 in RightAsync<Error, Unit>(unit)
        let plannedFiles = plannedSsl.Bind(s => Optional(s.CaCertificate)).ToSeq()
                           + plannedSsl.Bind(s => Optional(s.Certificate)).ToSeq()
                           + plannedSsl.Bind(s => Optional(s.PrivateKey)).ToSeq()
        let existingFiles = existingSsl.Values.ToSeq()
            .Bind(s => Optional(s.CaCertificate).ToSeq()
                       + Optional(s.Certificate).ToSeq()
                       + Optional(s.PrivateKey).ToSeq())
        from _2 in existingFiles.Except(plannedFiles).ToSeq()
            .Map(RemoveFile)
            .SequenceSerial()
        select unit;

    private EitherAsync<Error, Unit> RemoveFile(string path) =>
        Try(() =>
        {
            _systemEnvironment.FileSystem.DeleteFile(path);
            return unit;
        }).ToEitherAsync();

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
