using System.Net.Sockets;
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
    private readonly ISysEnvironment _sysEnv;

    public OVSDBProcess(ISysEnvironment sysEnv, OVSDbSettings dbSettings, ILogger logger) :
        base(sysEnv, OVSCommands.DBServer, dbSettings.ControlFile, dbSettings.LogFile, dbSettings.LoggingSettings, false, dbSettings.AllowAttach, logger)
    {
        _sysEnv = sysEnv;
        _dbSettings = dbSettings;
    }

    protected override string BuildArguments()
    {
        var baseArguments = base.BuildArguments();

        var dbFileFullPath = _sysEnv.FileSystem.ResolveOvsFilePath(_dbSettings.DBFile);
        var sb = new StringBuilder();
        sb.Append($"\"{dbFileFullPath}\"");
        sb.Append(' ');
        sb.Append($"--remote=\"{_dbSettings.Connection.GetCommandString(_sysEnv.FileSystem, true)}\"");
        sb.Append(' ');
        sb.Append(baseArguments);
        return sb.ToString();
    }
    
    public override EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        return EnsureDBFileCreated()
            .Bind(_ => base.Start(cancellationToken));
    }

    public EitherAsync<Error, bool> EnsureDBFileCreated()
    {
        var dbFileFullPath = _sysEnv.FileSystem.ResolveOvsFilePath(_dbSettings.DBFile);
        _sysEnv.FileSystem.EnsurePathForFileExists(dbFileFullPath);

        if (_sysEnv.FileSystem.FileExists(dbFileFullPath)) return false;

        var dbTool = new OVSDBTool(_sysEnv);
        return dbTool.CreateDBFile(_dbSettings.DBFile, _dbSettings.SchemaFile)
            .Map(_ => true);
    }
    
}