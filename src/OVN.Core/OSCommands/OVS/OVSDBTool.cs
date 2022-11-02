using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

/// <summary>
/// Commands for ovsdb-tool
/// </summary>
public class OVSDBTool : OVSTool
{
    private readonly ISysEnvironment _sysEnv;

    /// <summary>
    /// creates ovs tool instance for ovsdb-tool
    /// </summary>
    /// <param name="sysEnv"></param>
    public OVSDBTool(ISysEnvironment sysEnv) : base(sysEnv, OVSCommands.DBTool)
    {
        _sysEnv = sysEnv;
    }

    /// <summary>
    /// creates a new database file. 
    /// </summary>
    /// <param name="dbFile">database file</param>
    /// <param name="schemaFile">schema file to be used for new database.</param>
    /// <returns></returns>
    public EitherAsync<Error, Unit> CreateDBFile(OvsFile dbFile, OvsFile schemaFile)
    {
        var dbFilePath = _sysEnv.FileSystem.ResolveOvsFilePath(dbFile);
        var schemaPath = _sysEnv.FileSystem.ResolveOvsFilePath(schemaFile);

        var command = $"create \"{dbFilePath}\" \"{schemaPath}\"";
        return RunCommand(command).Map(_ => Unit.Default);
    }
}