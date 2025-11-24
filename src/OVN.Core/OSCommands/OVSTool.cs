using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands;

public class OVSTool: IOVSDBTool
{
    private readonly ISystemEnvironment _systemEnvironment;
    private readonly OvsFile _toolFile;

    protected OVSTool(ISystemEnvironment systemEnvironment, OvsFile toolFile)
    {
        _systemEnvironment = systemEnvironment;
        _toolFile = toolFile;
    }

    protected virtual string BuildArguments(string command)
    {
        return command;
    }

    protected EitherAsync<Error, string> RunCommandWithResponse(
        string command,
        CancellationToken cancellationToken = default)
    {
        return use(new OVSProcess(_systemEnvironment, _toolFile, BuildArguments(command)), 
            ovsProcess =>  ovsProcess.Start().ToAsync()
            .Bind(p =>
                p.WaitForExitWithResponse(cancellationToken)).ToEither(l => Error.New(l))
            .ToEither()).ToAsync();
    }
    
    protected EitherAsync<Error, int> RunCommand(
        string command,
        bool softWait = false,
        CancellationToken cancellationToken = default)
    {
         return use(new OVSProcess(_systemEnvironment, _toolFile, BuildArguments(command)), 
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
        Option<OVSParentReference> reference = default,
        CancellationToken cancellationToken = default) =>
        from _ in RightAsync<Error, Unit>(unit)
        let id = _systemEnvironment.GuidGenerator.GenerateGuid().ToString("D")
        from addToParentCommand in reference.Match(
            Some: r => from rowId in r.RowId.ToEitherAsync(Error.New("The parent row ID is missing."))
                       select $" -- add {r.TableName} {rowId} {r.RefColumn} {id}",
            None: () => "")
        let command = $"-- --id={id} create {tableName} {ColumnsValuesToCommandString(columns, true)}{addToParentCommand}"
        from result in RunCommandWithResponse(command, cancellationToken)
        select result;

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

        return RunCommandWithResponse(sb.ToString(), cancellationToken).Map(_ => Unit.Default);
    }

    /// <inheritdoc />
    public EitherAsync<Error, Option<T>> GetRecord<T>(
        string tableName,
        string rowId,
        Seq<string> columns = default,
        CancellationToken cancellationToken = default) where T : OVSTableRecord, new()
    {
        var sb = new StringBuilder();
        sb.Append("--if-exists");
        sb.Append(" --format json");

        if (!columns.IsEmpty)
            sb.Append($" --columns={ColumnsListToCommandString(columns)}");

        sb.Append($" list {tableName} {rowId}");

        return RunCommandWithResponse(sb.ToString(), cancellationToken)
            .Bind(json => ParseResponse<T>(json).ToAsync())
            .Map(e => e.HeadOrNone());
    }

    /// <inheritdoc />
    public EitherAsync<Error, Unit> RemoveRecord(
        string tableName,
        string rowId,
        CancellationToken cancellationToken = default) =>
        from _ in RunCommandWithResponse(
            $"destroy {tableName} {rowId}",
            cancellationToken)
        select unit;

    /// <inheritdoc />
    public EitherAsync<Error, Unit> RemoveColumnValue(
        string tableName,
        string rowId,
        string column,
        string value,
        CancellationToken cancellationToken = default) =>
        from _ in RunCommandWithResponse(
            $"remove {tableName} {rowId} {column} {value}",
            cancellationToken)
        select unit;

    /// <inheritdoc />
    public EitherAsync<Error, Seq<T>> FindRecords<T>(
        string tableName,
        Map<string, OVSQuery> query,
        Seq<string> columns = default,
        CancellationToken cancellationToken = default)
        where T : OVSTableRecord, new()
    {
        var sb = new StringBuilder();
        sb.Append("--format json");
        if (!columns.IsEmpty)
            sb.Append($" --columns={ColumnsListToCommandString(columns)}");

        sb.Append($" find {tableName} ");
        sb.Append(QueryCommandString(query));

        return RunCommandWithResponse(sb.ToString(), cancellationToken)
            .Bind(json => ParseResponse<T>(json).ToAsync());
    }

    private Either<Error, Seq<TRecord>> ParseResponse<TRecord>(
        string json)
        where TRecord : OVSTableRecord, new() =>
        from parsed in Try(() =>
        {
            if (string.IsNullOrEmpty(json))
                return new OVSJsonResponse();

            return JsonSerializer.Deserialize<OVSJsonResponse>(json)
                   ?? new OVSJsonResponse();
        }).ToEither(e => Error.New("Failed to deserialize OVS/OVN JSON response.", e))
        let columns = OVSEntityMetadata.Get(typeof(TRecord)).ToHashMap()
        let headings = parsed.Headings.ToSeq()
        let records = parsed.Records.ToSeq()
        from results in records
            .Map(r => ParseResponseRecord<TRecord>(r, headings, columns))
            .Sequence()
        select results;

    private Either<Error, TRecord> ParseResponseRecord<TRecord>(
        object responseRecord,
        Seq<String> headings,
        HashMap<string, OVSFieldMetadata> columns)
        where TRecord : OVSTableRecord, new() =>
        from recordValues in Try(() => ((JsonElement)responseRecord).Deserialize<object[]>().ToSeq())
            .ToEither(e => Error.New("The OVS/OVN response contains invalid data.", e))
        from _1 in guard(headings.Count == recordValues.Length,
            Error.New("The OVS/OVN data is invalid. The headings do not match the data."))
        let namesWithValues = headings.Zip(recordValues, (name, value) => (name, value))
        from parsedValues in namesWithValues
            .Map(t => ParseResponseRecordValue<TRecord>(t.name, t.value, columns))
            .Sequence()
        let result = OVSEntity.FromValueMap<TRecord>(parsedValues.Somes().ToMap())
        select result;

    private Either<Error, Option<(string Name, IOVSField Value)>> ParseResponseRecordValue<TRecord>(
        string name,
        object value,
        HashMap<string, OVSFieldMetadata> columns)
        where TRecord : OVSTableRecord, new() =>
        from result in columns.Find(name).Match(
            Some: c => ParseResponseRecordValue<TRecord>(name, value, c),
            None: () => Right<Error, Option<(string Name, IOVSField Value)>>(None))
        select result;

    private Either<Error, Option<(string Name, IOVSField Value)>> ParseResponseRecordValue<TRecord>(
        string name,
        object value,
        OVSFieldMetadata column)
        where TRecord : OVSTableRecord, new() =>
        from fieldValue in Try(() =>
        {
            var fv = OVSFieldActivator.JsonElementToOVSField(
                $"{typeof(TRecord).Name}:{name}",
                column.FieldType,
                (JsonElement)value);

            return Optional(fv);
        }).ToEither(e => Error.New("Failed to deserialize a value in the data of the OVS/OVN JSON response.", e))
        let result = fieldValue.Map(v => (name, v))
        select result;

    private class OVSJsonResponse
    {
        [JsonPropertyName("headings")] public string[]? Headings { get; init; }

        [JsonPropertyName("data")] public object[]? Records { get; init; }
    }
}
