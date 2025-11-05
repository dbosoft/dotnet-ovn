using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

    public EitherAsync<Error, string> ListDatabases(
        CancellationToken cancellationToken = default) =>
        RunCommandWithResponse(
            $"list-dbs {_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)}",
            cancellationToken);

    public EitherAsync<Error, string> DumpDatabase(
        string databaseName,
        CancellationToken cancellationToken = default) =>
        RunCommandWithResponse(
            $"dump --format=json {_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)} {databaseName}",
            cancellationToken);
}
