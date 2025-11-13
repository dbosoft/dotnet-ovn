using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using Dbosoft.OVN.Logging;
using Dbosoft.OVN.OSCommands.OVS;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands;

[PublicAPI]
public abstract class DemonProcessBase : IDisposable, IAsyncDisposable
{
    private readonly OvsFile _controlFile;
    private readonly OvsFile _logFile;
    private readonly OvsLoggingSettings _logging;
    private readonly bool _isOvn;
    private readonly OvsFile _exeFile;
    private readonly ILogger _logger;

    private readonly ISystemEnvironment _systemEnvironment;
    private OVSProcess? _ovsProcess;
    protected bool NoControlFileArgument = false;
    private bool _isStopping = false;
    private bool _allowAttached = false;
    
    protected DemonProcessBase(
        ISystemEnvironment systemEnvironment,
        OvsFile exeFile,
        OvsFile controlFile,
        OvsFile logFile,
        OvsLoggingSettings logging,
        bool isOvn,
        bool allowAttached,
        ILogger logger)
    {
        _systemEnvironment = systemEnvironment;
        _exeFile = exeFile;
        _controlFile = controlFile;
        _logFile = logFile;
        _logging = logging;
        _isOvn = isOvn;
        _logger = logger;
        _allowAttached = allowAttached;
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
        var controlFileFullPath = _systemEnvironment.FileSystem.ResolveOvsFilePath(_controlFile);
        var logFileFullName = _systemEnvironment.FileSystem.ResolveOvsFilePath(_logFile);
        var pidFileFullName = Path.ChangeExtension(_systemEnvironment.FileSystem.ResolveOvsFilePath(_controlFile), "pid");
        _systemEnvironment.FileSystem.EnsurePathForFileExists(_controlFile);
        _systemEnvironment.FileSystem.EnsurePathForFileExists(pidFileFullName);
        _systemEnvironment.FileSystem.EnsurePathForFileExists(_logFile);

        var sb = new StringBuilder();
        
        if (!NoControlFileArgument) 
            sb.Append($"--unixctl=\"{controlFileFullPath}\" ");

        sb.Append($"--pidfile=\"{pidFileFullName}\" ");

        sb.Append($"--log-file=\"{logFileFullName}\" ");
        sb.Append($"--verbose=\"file:{_logging.File.Level.ToOvsValue()}\"");
  
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
        
        sb.Append($"\"{_systemEnvironment.FileSystem.ResolveOvsFilePath(_exeFile, false)}\" ");
        sb.Append("--service --service-monitor ");
        sb.Append(arguments);

        return sb.ToString();
    }

    public virtual EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        async Task<Either<Error, Unit>> StartAsync()
        {
            if (_ovsProcess is { IsRunning: true }) return Unit.Default;
            
             var pidFileFullName = Path.ChangeExtension(_systemEnvironment.FileSystem.ResolveOvsFilePath(_controlFile), "pid");

            OVSProcess? orphanedDemon = default;
            if (_systemEnvironment.FileSystem.FileExists(pidFileFullName))
            {
                _logger.LogDebug("Existing pid file found. Trying to take control of orphaned demon");

                try
                {
                    var pidString = await _systemEnvironment.FileSystem.ReadFileAsync(pidFileFullName);
                    var pid = Convert.ToInt32(pidString);
                    orphanedDemon = new OVSProcess(_systemEnvironment, _exeFile, pid);
                    if (!orphanedDemon.IsRunning)
                        orphanedDemon = null;
                }
                catch (Exception)
                {
                    orphanedDemon = null;
                }
            }

            if (_allowAttached && orphanedDemon != null)
            {
                using var internalTokenSource = new CancellationTokenSource(5000);
                using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                    internalTokenSource.Token,
                    cancellationToken
                );

                _ovsProcess = orphanedDemon;
                var appControl = new OVSAppControl(_systemEnvironment, _controlFile);
                await appControl.SetLogging(_logging, cancelSource.Token).IfLeft(l =>
                {
                    _logger.LogDebug(
                        "Demon {OvsFile}:{ControlFile}: Response from ovs-appctl, while trying to update the logging settings: {Error}",
                        _exeFile.Name, _controlFile.Name, l);
                });
                _logger.LogInformation("Demon {OvsFile}:{ControlFile}: Successfully attached existing process, however logging will be unavailable for this process.",
                    _exeFile.Name, _controlFile.Name);
                return Unit.Default;
            }
            
            if (_systemEnvironment.FileSystem.FileExists(_controlFile))
            {
                using var internalTokenSource = new CancellationTokenSource(5000);
                using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                    internalTokenSource.Token,
                    cancellationToken
                );

                _logger.LogDebug("Existing control file found. Trying to stop orphaned demon.");
                
                var appControl = new OVSAppControl(_systemEnvironment, _controlFile);
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
                        _ = await orphanedDemon.KillAsync().IfFail(_ =>
                        {
                            _logger.LogError("Demon {ovsFile}:{controlFile}: Failed to kill a orphaned demon on same pid file.", _exeFile.Name, _controlFile.Name);
                        });
                    }
                }
            }

            var arguments = BuildArguments();
            _logger.LogTrace("Arguments for demon process {ovsFile}: {arguments}", _exeFile.Name, arguments);
            _ovsProcess = new OVSProcess(_systemEnvironment, _exeFile, arguments);
            _ovsProcess.AddMessageHandler(msg =>
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return;
                
                using var scope =
                    _logger.BeginScope("Demon {ovsFile}:{controlFile}", _exeFile.Name, _controlFile.Name);

                var logMessageParts = msg?.Split('|');
                
                if(logMessageParts?.Length>=5)
                {
                    var timeStampFound = DateTime.TryParse(logMessageParts[0], out var timestamp);
                    var logNumberFound = int.TryParse(logMessageParts[1], out var lineNumber);
                    var sender = logMessageParts[2];
                    var ovsLogLevel = OvsLogLevelExtensions.ParseOvsValue(logMessageParts[3]);

                    var msgBuilder = new StringBuilder();
                    for (var i = 4; i < logMessageParts.Length; i++)
                    {
                        msgBuilder.Append(logMessageParts[i]);
                        msgBuilder.Append('|');
                    }

                    var message = msgBuilder.ToString().TrimEnd('|');

                    if (timeStampFound && logNumberFound && ovsLogLevel.IsSome)
                    {
                         var logLevel = ovsLogLevel.ValueUnsafe() switch
                        {
                            OvsLogLevel.Emergency => LogLevel.Error,
                            OvsLogLevel.Error => LogLevel.Information,
                            OvsLogLevel.Warning => LogLevel.Information,
                            OvsLogLevel.Info => LogLevel.Debug,
                            OvsLogLevel.Debug => LogLevel.Trace,
                            _ => LogLevel.None
                        };

                         // update level of some known messages
                         if (message.Contains("seconds ago) due to excessive rate") ||
                             message.Contains("another ovs-vswitchd process is running, disabling this process"))
                         {
                             logLevel = LogLevel.Trace;
                         }

                         if (message.Contains("could not open network device"))
                         {
                             logLevel = LogLevel.Warning;
                         }

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

                _logger.LogTrace("{message}", msg);
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
                ? new OVNAppControl(_systemEnvironment, _controlFile)
                : new OVSAppControl(_systemEnvironment, _controlFile);

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
                .MapLeftAsync(async l =>
                {
                    if (_ovsProcess.IsRunning && ensureNodeStopped)
                    {
                        _logger.LogInformation(
                            "Demon {ovsFile}:{controlFile}: graceful stop failed - process will be killed",
                            _exeFile.Name, _controlFile.Name);
                        
                        if (_ovsProcess is not null)
                            await _ovsProcess.KillAsync();
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
                ? new OVNAppControl(_systemEnvironment, _controlFile) 
                : new OVSAppControl(_systemEnvironment, _controlFile);
            
            // check alive version check should never user parent timeout as it may already timed out
            var checkCancelSource = new CancellationTokenSource(2000);
            var version = await appControl.GetVersion(checkCancelSource.Token)
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
                _ = await _ovsProcess.KillAsync().IfFail(e =>
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

    /// <summary>
    /// disconnects from watched process
    /// </summary>
    /// <returns></returns>
    public EitherAsync<Error, Unit> Disconnect()
    {
        _ovsProcess?.Dispose();
        _ovsProcess = null;
        return Unit.Default;
    }
}