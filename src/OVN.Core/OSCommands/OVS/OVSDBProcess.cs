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
        base(sysEnv, OVSCommands.DBServer, dbSettings.ControlFile, logger)
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

    public async Task<Either<Error, bool>> WaitForDbSocket(CancellationToken cancellationToken)
    {
        return await Prelude.TryAsync(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_dbSettings.Connection.PipeFile != null)
                {
                    if (_sysEnv.FileSystem.FileExists(_dbSettings.Connection.PipeFile))
                        return true;
                }
                else
                {
                    using var tcpClient = new TcpClient();
                    try
                    {
                        await tcpClient.ConnectAsync(
                            _dbSettings.Connection.Address ?? "127.0.0.1",
                            _dbSettings.Connection.Port, cancellationToken);
                        return true;
                    }
                    catch (Exception)
                    {
                        // ignored, port closed
                    }
                }

                await Task.Delay(500, cancellationToken);
            }

            return false;
        }).ToEither(l => Error.New(l)).Map(_ => true);
    }
}