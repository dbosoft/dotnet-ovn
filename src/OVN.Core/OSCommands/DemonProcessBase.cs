using System.Text;
using Dbosoft.OVN.OSCommands.OVS;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands;

[PublicAPI]
public abstract class DemonProcessBase : IDisposable, IAsyncDisposable
{
    private readonly OvsFile _controlFile;
    private readonly OvsFile _exeFile;
    private readonly ILogger _logger;

    private readonly ISysEnvironment _sysEnv;
    private OVSProcess? _ovsProcess;
    protected bool NoControlFileArgument = false;
    
    protected DemonProcessBase(
        ISysEnvironment sysEnv, OvsFile exeFile, OvsFile controlFile,
        ILogger logger)
    {
        _sysEnv = sysEnv;
        _exeFile = exeFile;
        _controlFile = controlFile;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var cts = new CancellationTokenSource(5000);
        await Stop(cts.Token);
        Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual string BuildArguments()
    {
        var controlFileFullPath = _sysEnv.FileSystem.ResolveOvsFilePath(_controlFile);
        var pidFileFullName = Path.ChangeExtension(_sysEnv.FileSystem.ResolveOvsFilePath(_controlFile), "pid");
        _sysEnv.FileSystem.EnsurePathForFileExists(_controlFile);
        _sysEnv.FileSystem.EnsurePathForFileExists(pidFileFullName);
        
        if (NoControlFileArgument) 
            return $"--pidfile=\"{pidFileFullName}\"";
        
        return $"--unixctl=\"{controlFileFullPath}\" --pidfile=\"{pidFileFullName}\"";

    }
    
    

    private void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (_ovsProcess is { IsRunning: true })
        {
            _logger.LogWarning(
                "Demon {ovsFile}:{controlFile}: Demon is disposing, but process is still running. Killing process.",
                _exeFile.Name, _controlFile.Name);
            _ovsProcess?.Kill();
        }

        _ovsProcess?.Dispose();
        _ovsProcess = null;
    }
    
    public string GetServiceCommand()
    {
        var arguments = BuildArguments();
        var sb = new StringBuilder();
        
        sb.Append($"\"{_sysEnv.FileSystem.ResolveOvsFilePath(_exeFile, false)}\" ");
        sb.Append("--service --service-monitor ");
        sb.Append(arguments);

        return sb.ToString();
    }

    public virtual EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        async Task<Either<Error, Unit>> StartAsync()
        {
            if (_ovsProcess is { IsRunning: true }) return Unit.Default;
            
            
            if (_sysEnv.FileSystem.FileExists(_controlFile))
            {
                _logger.LogTrace("Existing control file found. Trying to stop orphaned demon.");

                var appControl = new OVSAppControl(_sysEnv, _controlFile);
                await appControl.StopApp(cancellationToken).IfLeft(l =>
                {
                    _logger.LogDebug(
                        "Demon {ovsFile}:{controlFile}: Response from ovs-appctl, while trying to stop orphaned process: {error}",
                        _exeFile.Name, _controlFile.Name, l);
                });
            }

            var arguments = BuildArguments();
            _logger.LogTrace("Arguments for demon process {ovsFile}: {arguments}", _exeFile.Name, arguments);
            _ovsProcess = new OVSProcess(_sysEnv, _exeFile, arguments);
            _ovsProcess.AddMessageHandler(msg =>
            {
                using var scope =
                    _logger.BeginScope("Demon {ovsFile}:{controlFile}", _exeFile.Name, _controlFile.Name);
                _logger.LogDebug("{message}", msg);
            });

            return _ovsProcess.Start().ToEither(Error.New).Map(_ =>
            {
                _logger.LogDebug("Demon {ovsFile}:{controlFile} started", _exeFile.Name, _controlFile.Name);
                return Unit.Default;
            });
        }

        return StartAsync().ToAsync();
    }

    public virtual EitherAsync<Error, Unit> Stop(CancellationToken cancellationToken)
    {
        if (_ovsProcess is not { IsRunning: true })
        {
            _ovsProcess?.Dispose();
            _ovsProcess = null;
            return Unit.Default;
        }

        var appCtrl = new OVSAppControl(_sysEnv, _controlFile);
        return appCtrl.StopApp(cancellationToken)
            .Bind(_ => _ovsProcess.WaitForExit(cancellationToken).ToEither(l => Error.New(l))
                .Map(_ =>
                {
                    _logger.LogDebug("Demon {ovsFile}:{controlFile} stopped", _exeFile.Name, _controlFile.Name);
                    _ovsProcess?.Dispose();
                    _ovsProcess = null;
                    return Unit.Default;
                }));
    }


    public EitherAsync<Error, bool> CheckAlive(
        bool checkResponse,
        bool canRestart = true,
        CancellationToken cancellationToken = default)
    {
        async Task<Either<Error, bool>> CheckAliveAsync()
        {
            if (_ovsProcess is not { IsRunning: true })
            {
                _logger.LogTrace("Demon {ovsFile}:{controlFile}: check alive detected stopped process.",
                    _exeFile.Name, _controlFile.Name);
                return canRestart ? await Start(cancellationToken).Map(_ => true) : false;
            }

            if (!checkResponse)
                return true;

            var appControl = new OVSAppControl(_sysEnv, _controlFile);
            var version = await appControl.GetVersion(cancellationToken)
                .Match(r => r, l =>
                {
                    _logger.LogDebug("Process {ovsFile}:{controlFile}: AppControl error: {error}", _exeFile.Name,
                        _controlFile.Name, l);
                    return "";
                });

            if (string.IsNullOrWhiteSpace(version))
            {
                _logger.LogTrace("Process {ovsFile}:{controlFile}: check alive retrieved empty version responds.",
                    _exeFile.Name, _controlFile.Name);


                //not possible to contact process, kill it and restart
                _ = _ovsProcess.Kill()
                    .IfFail(e =>
                    {
                        _logger.LogDebug(e, "{ovsFile}:{controlFile}: Failed to kill process.", _exeFile.Name,
                            _controlFile.Name);
                        return Unit.Default;
                    });


                return canRestart ? await Start(cancellationToken).Map(_ => true) : false;
            }

            _logger.LogTrace("Process {ovsFile}:{controlFile}: check alive retrieved version response '{version}'",
                _exeFile.Name, _controlFile.Name, version);


            return true;
        }

        return CheckAliveAsync().ToAsync();
    }
}