using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

/// <summary>
/// Commands for <c>ovsdb-client</c>
/// </summary>
public class OVSDbClientTool : OVSTool
{
    private readonly ISystemEnvironment _systemEnvironment;
    private readonly OvsDbConnection _dbConnection;

    public OVSDbClientTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection)
        : base(systemEnvironment, OVSCommands.DBClient)
    {
        _systemEnvironment = systemEnvironment;
        _dbConnection = dbConnection;
    }

    public EitherAsync<Error, string> PrintDatabase(
        string databaseName,
        CancellationToken cancellationToken = default) =>
        RunCommandWithResponse(
            $"dump {_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)} {databaseName}",
            cancellationToken);

    public EitherAsync<Error, string> GetSchemaVersion(
        string databaseName,
        CancellationToken cancellationToken = default) =>
        RunCommandWithResponse(
            $"get-schema-version {_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)} {databaseName}",
            cancellationToken);
}
