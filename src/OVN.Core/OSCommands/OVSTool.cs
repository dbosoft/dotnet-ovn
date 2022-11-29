using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands;

public class OVSTool: IOVSDBTool
{

    private static readonly Random Random = new();
    private readonly ISysEnvironment _sysEnv;
    private readonly OvsFile _toolFile;

    protected OVSTool(ISysEnvironment sysEnv, OvsFile toolFile)
    {
        _sysEnv = sysEnv;
        _toolFile = toolFile;
    }

    protected virtual string BuildArguments(string command)
    {
        return command;
    }

    protected EitherAsync<Error, string> RunCommandWithResponse(string command, CancellationToken cancellationToken = default)
    {
        return Prelude.use(new OVSProcess(_sysEnv, _toolFile, BuildArguments(command)), 
            ovsProcess =>  ovsProcess.Start().ToAsync()
            .Bind(p =>
                p.WaitForExitWithResponse(cancellationToken)).ToEither(l => Error.New(l))
            .ToEither()).ToAsync();
    }
    
    protected EitherAsync<Error, int> RunCommand(string command, bool softWait = false, CancellationToken cancellationToken = default)
    {
         return Prelude.use(new OVSProcess(_sysEnv, _toolFile, BuildArguments(command)), 
            ovsProcess => 
                ovsProcess.Start().ToAsync()
                .Bind(p =>
                    p.WaitForExit(softWait,cancellationToken))
                .ToEither(l => Error.New(l)).ToEither()).ToAsync();
}

    private static string ColumnsValuesToCommandString(Map<string, IOVSField> columns, bool setMode)
    {
        var sb = new StringBuilder();
        foreach (var column in columns) sb.Append($"{column.Value.GetColumnValue(column.Key, setMode)} ");

        return sb.ToString().TrimEnd();
    }

    private static string ColumnsListToCommandString(IEnumerable<string> columns)
    {
        var sb = new StringBuilder();
        foreach (var column in columns) sb.Append($"{column},");

        return sb.ToString().TrimEnd(',');
    }

    private static string QueryCommandString(Map<string, OVSQuery> columns)
    {
        var sb = new StringBuilder();
        foreach (var column in columns)
            sb.Append($"{column.Value.Field.GetQueryString(column.Key, column.Value.Option)}");

        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc />
    public EitherAsync<Error, string> CreateRecord(
        string tableName,
        Map<string, IOVSField> columns,
        OVSParentReference? reference = default,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (reference != null)
            sb.Append(" -- --id=@ref ");

        sb.Append($"create {tableName} ");
        sb.Append(ColumnsValuesToCommandString(columns, true));

        if (reference.HasValue)
            sb.Append($" -- add {reference.Value.TableName} {reference.Value.RowId} {reference.Value.RefColumn} @ref");

        return RunCommandWithResponse(sb.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public EitherAsync<Error, Unit> UpdateRecord(
        string tableName,
        string rowId,
        Map<string, IOVSField> columnsToAdd,
        Map<string, IOVSField> columnsToSet,
        IEnumerable<string> columnsToClear,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (columnsToAdd.Count > 0)
            sb.Append($" -- add {tableName} {rowId} {ColumnsValuesToCommandString(columnsToAdd, false)}");

        if (columnsToSet.Count > 0)
            sb.Append($" -- set {tableName} {rowId} {ColumnsValuesToCommandString(columnsToSet, true)}");

        var toClear = columnsToClear as string[] ?? columnsToClear.ToArray();

        if (toClear.Any())
            sb.Append($" -- clear {tableName} {rowId} {ColumnsListToCommandString(toClear).Replace(',', ' ')}");


        return RunCommand(sb.ToString(), false, cancellationToken).Map(_ => Unit.Default);
    }

    /// <inheritdoc />
    public EitherAsync<Error, T> GetRecord<T>(
        string tableName,
        string rowId,
        IEnumerable<string>? columns = default,
        Map<Guid, Map<string, IOVSField>> additionalFields = default,
        CancellationToken cancellationToken = default) where T : OVSTableRecord, new()
    {
        var sb = new StringBuilder();
        sb.Append("--format json");

        if (columns != null)
            sb.Append($" --columns={ColumnsListToCommandString(columns)}");

        sb.Append($" list {tableName} {rowId}");

        return RunCommandWithResponse(sb.ToString(), cancellationToken)
            .Bind(r => MapResponse<T>(r, additionalFields))
            .Bind(e =>
                e.HeadOrLeft(Error.New(Errors.SequenceEmpty)).ToAsync());
    }

    /// <inheritdoc />
    public EitherAsync<Error, Unit> RemoveRecord(
        string tableName,
        string rowId,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append($"destroy {tableName} {rowId}");

        return RunCommand(sb.ToString(), false, cancellationToken)
            .Map(_ => Unit.Default);
    }

    /// <inheritdoc />
    public EitherAsync<Error, Unit> RemoveColumnValue(
        string tableName,
        string rowId,
        string column,
        string value,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append($"remove {tableName} {rowId} {column} {value}");

        return RunCommand(sb.ToString(),false, cancellationToken)
            .Map(_ => Unit.Default);
    }

    private static EitherAsync<Error, Seq<T>> MapResponse<T>(
        string jsonResponse,
        Map<Guid, Map<string, IOVSField>> additionalFields = default
    ) where T : OVSTableRecord, new()
    {
        return Prelude.Try(() =>
        {
            if(string.IsNullOrWhiteSpace(jsonResponse))
                return Prelude.RightAsync<Error, Seq<T>>(Seq<T>.Empty);
            
            var response = JsonSerializer.Deserialize<OVSJsonResponse>(jsonResponse);

            return response == null
                ? Prelude.LeftAsync<Error, Seq<T>>(Error.New("Failed to deserialize OVN json response"))
                : Prelude.RightAsync<Error, Seq<T>>(response.ToOVSEntities<T>(additionalFields));
        }).ToEitherAsync().Flatten();
    }

    /// <inheritdoc />
    public EitherAsync<Error, Seq<T>> FindRecords<T>(
        string tableName,
        Map<string, OVSQuery> query,
        IEnumerable<string>? columns = default,
        Map<Guid, Map<string, IOVSField>> additionalFields = default,
        CancellationToken cancellationToken = default) where T : OVSTableRecord, new()
    {
        var sb = new StringBuilder();
        sb.Append("--format json");
        if (columns != null)
            sb.Append($" --columns={ColumnsListToCommandString(columns)}");

        sb.Append($" find {tableName} ");
        sb.Append(QueryCommandString(query));

        return RunCommandWithResponse(sb.ToString(), cancellationToken)
            .Bind(r => MapResponse<T>(r, additionalFields));
    }

    private class OVSJsonResponse
    {
        [JsonPropertyName("headings")] public string[]? Headings { get; set; }

        [JsonPropertyName("data")] public object[]? Records { get; set; }

        private Seq<Map<string, IOVSField>> ToOVSEntitiesValueMap<TEntity>()
            where TEntity : OVSEntity
        {
            var result = new List<Map<string, IOVSField>>();

            var columns = OVSEntityMetadata.Get(typeof(TEntity));

            if (Records == null || Headings == null)
                return Seq<Map<string, IOVSField>>.Empty;

            foreach (var record in Records)
            {
                if (record is not JsonElement recordElement)
                    continue;

                var dictionary = new Dictionary<string, IOVSField>(Headings.Length);
                var recordValues = recordElement.Deserialize<object[]>();
                if (recordValues == null)
                    continue;

                for (var i = 0; i < Headings.Length; i++)
                {
                    var recordValueElement = (JsonElement)recordValues[i];
                    var columnName = Headings[i];
                    if (!columns.ContainsKey(columnName)) continue;
                    var metadata = columns[columnName];

                    var value = OVSFieldActivator.JsonElementToOVSField(
                        $"{typeof(TEntity).Name}:{columnName}",
                        metadata.FieldType, recordValueElement);
                    if (value != null)
                        dictionary.Add(columnName, value);
                }

                result.Add(dictionary.ToMap());
            }

            return result.ToSeq();
        }

        public Seq<T> ToOVSEntities<T>(Map<Guid, Map<string, IOVSField>> additionalFields = default)
            where T : OVSTableRecord, new()
        {
            var ovsEntities = ToOVSEntitiesValueMap<T>();
            return ovsEntities
                .Map(m =>
                {
                    if (!m.ContainsKey("_uuid") || m["_uuid"] is not OVSValue<Guid> idValue)
                        return m;

                    return !additionalFields.ContainsKey(idValue.Value)
                        ? m
                        : m.AddRange(additionalFields[idValue.Value]);
                })
                .Map(OVSEntity.FromValueMap<T>);
        }
    }
}