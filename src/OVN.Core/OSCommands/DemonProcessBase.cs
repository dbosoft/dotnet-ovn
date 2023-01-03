using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
    private readonly bool _isOvn;
    private readonly OvsFile _exeFile;
    private readonly ILogger _logger;

    private readonly ISysEnvironment _sysEnv;
    private OVSProcess? _ovsProcess;
    protected bool NoControlFileArgument = false;
    private bool _isStopping = false;
    
    protected DemonProcessBase(
        ISysEnvironment sysEnv, OvsFile exeFile, OvsFile controlFile,
        bool isOvn,
        ILogger logger)
    {
        _sysEnv = sysEnv;
        _exeFile = exeFile;
        _controlFile = controlFile;
        _isOvn = isOvn;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var cts = new CancellationTokenSource(5000);
        await Stop(false,cts.Token);
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

        var sb = new StringBuilder();
        
        if (!NoControlFileArgument) 
            sb.Append($"--unixctl=\"{controlFileFullPath}\" ");

        sb.Append($"--pidfile=\"{pidFileFullName}\"");
  
        return sb.ToString();

    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing) return;
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
            
             var pidFileFullName = Path.ChangeExtension(_sysEnv.FileSystem.ResolveOvsFilePath(_controlFile), "pid");

            OVSProcess? orphanedDemon = default;
            if (_sysEnv.FileSystem.FileExists(pidFileFullName))
            {
                _logger.LogDebug("Existing pid file found. Trying to take control of orphaned demon");

                try
                {
                    var pidString = _sysEnv.FileSystem.ReadFileAsString(pidFileFullName);
                    var pid = Convert.ToInt32(pidString);
                    orphanedDemon = new OVSProcess(_sysEnv, _exeFile, pid);
                    if (!orphanedDemon.IsRunning)
                        orphanedDemon = null;
                }
                catch (Exception)
                {
                    orphanedDemon = null;
                }
            }
            
            var internalTokenSource =
                new CancellationTokenSource(5000);
            var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                internalTokenSource.Token,
                cancellationToken
            );
            
            if (_sysEnv.FileSystem.FileExists(_controlFile))
            {
                _logger.LogDebug("Existing control file found. Trying to stop orphaned demon.");
                
                var appControl = new OVSAppControl(_sysEnv, _controlFile);
                await appControl.StopApp(cancelSource.Token).IfLeft(l =>
                {
                    _logger.LogDebug(
                        "Demon {ovsFile}:{controlFile}: Response from ovs-appctl, while trying to stop orphaned process: {error}",
                        _exeFile.Name, _controlFile.Name, l);
                });
                
                internalTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                
                //try to wait for stop, kill it if not reacting to stop
                if (orphanedDemon != null)
                {
                    _logger.LogTrace("Demon {ovsFile}:{controlFile}: Wait for orphaned demon to be stopped.", _exeFile.Name, _controlFile.Name);
                    var waitResult = await orphanedDemon.WaitForExit(false, cancelSource.Token)();
                    if (waitResult.IsFaulted)
                    {
                        _logger.LogWarning("Demon {ovsFile}:{controlFile}: Failed to stop a orphaned demon on same pid file. Killing orphaned demon.", _exeFile.Name, _controlFile.Name);
                        orphanedDemon.Kill().IfFail(_ =>
                        {
                            _logger.LogError("Demon {ovsFile}:{controlFile}: Failed to kill a orphaned demon on same pid file.", _exeFile.Name, _controlFile.Name);
                        });
                    }
                }
            }

            var arguments = BuildArguments();
            _logger.LogTrace("Arguments for demon process {ovsFile}: {arguments}", _exeFile.Name, arguments);
            _ovsProcess = new OVSProcess(_sysEnv, _exeFile, arguments);
            _ovsProcess.AddMessageHandler(msg =>
            {
                using var scope =
                    _logger.BeginScope("Demon {ovsFile}:{controlFile}", _exeFile.Name, _controlFile.Name);

                var logMessageParts = msg?.Split('|');
                
                if(logMessageParts?.Length>=5)
                {
                    var timeStampFound = DateTime.TryParse(logMessageParts[0], out var timestamp);
                    var logNumberFound = int.TryParse(logMessageParts[1], out var lineNumber);
                    var sender = logMessageParts[2];
                    var hasLogLevel = Enum.TryParse<OvsLogLevel>(logMessageParts[3], true, out var ovsLogLevel);

                    var msgBuilder = new StringBuilder();
                    for (var i = 4; i < logMessageParts.Length; i++)
                    {
                        msgBuilder.Append(logMessageParts[i]);
                        msgBuilder.Append('|');
                    }

                    var message = msgBuilder.ToString().TrimEnd('|');

                    if (timeStampFound && logNumberFound && hasLogLevel)
                    {
                        var logLevel = ovsLogLevel switch
                        {
                            OvsLogLevel.emer => LogLevel.Error,
                            OvsLogLevel.err => LogLevel.Information,
                            OvsLogLevel.warn => LogLevel.Information,
                            OvsLogLevel.info => LogLevel.Debug,
                            OvsLogLevel.dbg => LogLevel.Trace,
                            _ => LogLevel.None
                        };
                        
                        using (_logger.BeginScope(new Dictionary<string, object>{
                                   ["ovsLogLevel"] = ovsLogLevel,
                                   ["ovsTimeStamp"] = timestamp,
                                   ["ovsLogNo"] = lineNumber,
                                   ["ovsSender"] = sender
                               }))
                            _logger.Log(logLevel, "{message}", message);
                        
                        return;
                    }
                    
                }

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

    public virtual EitherAsync<Error, Unit> Stop(bool ensureNodeStopped, CancellationToken cancellationToken)
    {
        if (_ovsProcess is not { IsRunning: true })
        {
            _ovsProcess?.Dispose();
            _ovsProcess = null;
            return Unit.Default;
        }

        _isStopping = true;
        try
        {
            IAppControl appCtrl = _isOvn
                ? new OVNAppControl(_sysEnv, _controlFile)
                : new OVSAppControl(_sysEnv, _controlFile);

            var internalTokenSource =
                new CancellationTokenSource(2000);
            var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                internalTokenSource.Token,
                cancellationToken);            
            
            return appCtrl.StopApp(cancelSource.Token)
                .Bind(_ =>
                {
                    internalTokenSource.CancelAfter(new TimeSpan(0,0,5));
                    return _ovsProcess.WaitForExit(!ensureNodeStopped, cancelSource.Token).ToEither(l => Error.New(l))
                        .Map(_ =>
                        {
                            _logger.LogDebug("Demon {ovsFile}:{controlFile} stopped", _exeFile.Name, _controlFile.Name);
                            _ovsProcess?.Dispose();
                            _ovsProcess = null;
                            return Unit.Default;
                        });
                })
                .MapLeft(l =>
                {
                    if (_ovsProcess.IsRunning && ensureNodeStopped)
                    {
                        _logger.LogInformation(
                            "Demon {ovsFile}:{controlFile}: graceful stop failed - process will be killed",
                            _exeFile.Name, _controlFile.Name);
                        _ovsProcess?.Kill();
                    }

                    _ovsProcess?.Dispose();
                    _ovsProcess = null;
                    return l;
                });
        }
        finally
        {
            _isStopping = false;
        }
    }

    private int _checkAliveFailedCounter = 0;

    public EitherAsync<Error, bool> CheckAlive(
        bool checkResponse,
        bool canRestart = true,
        CancellationToken cancellationToken = default)
    {
        async Task<Either<Error, bool>> CheckAliveAsync()
        {
            if (_isStopping)
                return true;
            
            if (_ovsProcess is not { IsRunning: true })
            {
                _logger.LogTrace("Demon {ovsFile}:{controlFile}: check alive detected stopped process.",
                    _exeFile.Name, _controlFile.Name);
                return canRestart ? await Start(cancellationToken).Map(_ => true) : false;
            }

            if (!checkResponse)
                return true;

            IAppControl appControl = _isOvn 
                ? new OVNAppControl(_sysEnv, _controlFile) 
                : new OVSAppControl(_sysEnv, _controlFile);
            
            var version = await appControl.GetVersion(cancellationToken)
                .Match(r => r, l =>
                {
                    _logger.LogDebug("Process {ovsFile}:{controlFile}: AppControl error: {error}", _exeFile.Name,
                        _controlFile.Name, l);
                    return "";
                });
            
            if (string.IsNullOrWhiteSpace(version))
            {
                _logger.LogTrace(
                    "Process {ovsFile}:{controlFile}: check alive retrieved empty version responds.",
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

    private enum OvsLogLevel
    {
        emer,
        err,
        warn,
        info,
        dbg
    }
}