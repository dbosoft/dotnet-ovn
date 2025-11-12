using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

/// <summary>
/// Commands for ovsdb-tool
/// </summary>
public class OVSDBTool : OVSTool
{
    private readonly ISystemEnvironment _systemEnvironment;

    /// <summary>
    /// creates ovs tool instance for ovsdb-tool
    /// </summary>
    /// <param name="systemEnvironment"></param>
    public OVSDBTool(ISystemEnvironment systemEnvironment)
        : base(systemEnvironment, OVSCommands.DBTool)
    {
        _systemEnvironment = systemEnvironment;
    }

    /// <summary>
    /// creates a new database file. 
    /// </summary>
    /// <param name="dbFile">database file</param>
    /// <param name="schemaFile">schema file to be used for new database.</param>
    /// <returns></returns>
    public EitherAsync<Error, Unit> CreateDBFile(OvsFile dbFile, OvsFile schemaFile)
    {
        var dbFilePath = _systemEnvironment.FileSystem.ResolveOvsFilePath(dbFile);
        var schemaPath = _systemEnvironment.FileSystem.ResolveOvsFilePath(schemaFile);

        var command = $"create \"{dbFilePath}\" \"{schemaPath}\"";
        return RunCommandWithResponse(command).Map(_ => Unit.Default);
    }
}