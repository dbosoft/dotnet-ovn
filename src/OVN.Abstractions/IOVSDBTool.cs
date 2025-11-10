using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

/// <summary>
/// Abstraction of OVS Database Tool
/// </summary>
public interface IOVSDBTool
{
    /// <summary>
    /// Creates a database record. If a reference is specified the new record
    /// will be added to specified referencing column. 
    /// </summary>
    /// <param name="tableName">Name of Table</param>
    /// <param name="columns">Columns of new record</param>
    /// <param name="reference">reference where new record should be added</param>
    /// <param name="cancellationToken"></param>
    /// <returns>UUID of new record</returns>
    EitherAsync<Error, string> CreateRecord(
        string tableName,
        Map<string, IOVSField> columns,
        Option<OVSParentReference> reference = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a database record. 
    /// </summary>
    /// <param name="tableName">Name of table</param>
    /// <param name="rowId">UUID or name column if supported by table</param>
    /// <param name="columnsToAdd">columns to add for row</param>
    /// <param name="columnsToSet">columns to set for row</param>
    /// <param name="columnsToClear">columns to clear</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    EitherAsync<Error, Unit> UpdateRecord(
        string tableName,
        string rowId,
        Map<string, IOVSField> columnsToAdd,
        Map<string, IOVSField> columnsToSet,
        IEnumerable<string> columnsToClear,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a record from database table and returns it as <see cref="OVSTableRecord"/>.
    /// Returns <see cref="OptionNone"/> when the record does not exist.
    /// </summary>
    /// <param name="tableName">table name</param>
    /// <param name="rowId">UUID or name column if supported by table</param>
    /// <param name="columns">columns to be read. If not specified all columns of entity will be requested.</param>
    /// <param name="additionalFields">map of additional fields that should be added to created entity.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <returns>created entity</returns>
    EitherAsync<Error, Option<T>> GetRecord<T>(
        string tableName,
        string rowId,
        IEnumerable<string>? columns = default,
        Map<Guid, Map<string, IOVSField>> additionalFields = default,
        CancellationToken cancellationToken = default) where T : OVSTableRecord, new();

    /// <summary>
    /// Removes a record from database.
    /// </summary>
    /// <param name="tableName">table name</param>
    /// <param name="rowId">UUID or name column if supported by table</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Unit</returns>
    EitherAsync<Error, Unit> RemoveRecord(
        string tableName,
        string rowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes specified value from a record column.
    /// Used to remove references.
    /// </summary>
    /// <param name="tableName">table name</param>
    /// <param name="rowId">UUID or name column if supported by table</param>
    /// <param name="column">Column where value should be removed</param>
    /// <param name="value">Value to be removed</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    EitherAsync<Error, Unit> RemoveColumnValue(
        string tableName,
        string rowId,
        string column,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query database for table records and returns them as entity sequence.
    /// </summary>
    /// <param name="tableName">table name</param>
    /// <param name="query">Query to find records.</param>
    /// <param name="columns">Columns to be included in entity.</param>
    /// <param name="additionalFields">map of additional fields that should be added to created entities.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">type of entities</typeparam>
    /// <returns>Sequence of entities</returns>
    EitherAsync<Error, Seq<T>> FindRecords<T>(
        string tableName,
        Map<string, OVSQuery> query,
        Seq<string> columns = default,
        CancellationToken cancellationToken = default) where T : OVSTableRecord, new();
}