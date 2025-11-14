using System.Text;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

[PublicAPI]
public class OVSDBProcess : DemonProcessBase
{
    private readonly OVSDbSettings _dbSettings;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVSDBProcess(
        ISystemEnvironment systemEnvironment,
        OVSDbSettings dbSettings,
        ILogger logger)
        : base(systemEnvironment,
            OVSCommands.DBServer,
            dbSettings.ControlFile,
            dbSettings.LogFile,
            dbSettings.LoggingSettings,
            false,
            dbSettings.AllowAttach,
            logger)
    {
        _systemEnvironment = systemEnvironment;
        _dbSettings = dbSettings;
    }

    protected override string BuildArguments()
    {
        var sb = new StringBuilder();
        var dbFileFullPath = _systemEnvironment.FileSystem.ResolveOvsFilePath(_dbSettings.DBFile);
        sb.Append($"\"{dbFileFullPath}\" ");
        
        sb.Append($"--remote=\"{_dbSettings.Connection.GetCommandString(_systemEnvironment.FileSystem, true)}\" ");
        if (_dbSettings.UseRemotesFromDatabase)
            sb.Append($"--remote=\"db:{_dbSettings.DatabaseName},{_dbSettings.GlobalTableName},connections\" ");

        sb.Append($"--private-key=\"db:{_dbSettings.DatabaseName},SSL,private_key\" ");
        sb.Append($"--certificate=\"db:{_dbSettings.DatabaseName},SSL,certificate\" ");
        sb.Append($"--ca-cert=\"db:{_dbSettings.DatabaseName},SSL,ca_cert\" ");

        sb.Append(base.BuildArguments());
        return sb.ToString();
    }
    
    public override EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        return EnsureDBFileCreated()
            .Bind(_ => base.Start(cancellationToken));
    }

    public EitherAsync<Error, bool> EnsureDBFileCreated()
    {
        var dbFileFullPath = _systemEnvironment.FileSystem.ResolveOvsFilePath(_dbSettings.DBFile);
        _systemEnvironment.FileSystem.EnsurePathForFileExists(dbFileFullPath);

        if (_systemEnvironment.FileSystem.FileExists(dbFileFullPath)) return false;

        var dbTool = new OVSDBTool(_systemEnvironment);
        return dbTool.CreateDBFile(_dbSettings.DBFile, _dbSettings.SchemaFile)
            .Map(_ => true);
    }
}