using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.OSCommands;

[PublicAPI]
public class OVSProcess : IDisposable
{
    private readonly string _arguments;
    private readonly OvsFile _exeFile;
    private readonly List<Action<string?>> _messageHandlers = new();
    private readonly ISystemEnvironment _systemEnvironment;
    private IProcess? _startedProcess;
    private bool _canBeStarted = true;
    private bool _canBeRedirected = true;
    
    public OVSProcess(
        ISystemEnvironment systemEnvironment,
        OvsFile exeFile,
        string arguments = "")
    {
        _systemEnvironment = systemEnvironment;
        _exeFile = exeFile;
        _arguments = arguments;
    }
    
    public OVSProcess(
        ISystemEnvironment systemEnvironment,
        OvsFile exeFile,
        int processId)
    {
        _systemEnvironment = systemEnvironment;
        _startedProcess = _systemEnvironment.CreateProcess(processId);
        _canBeStarted = false;
        _exeFile = exeFile;
        _canBeRedirected = false;
        if (_startedProcess.ProcessName != exeFile.Name)
            throw new InvalidOperationException($"Process {_startedProcess.ProcessName} is not same as {exeFile.Name}");
    }

    public bool IsRunning => _startedProcess is { HasExited: false };

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    

    public Try<OVSProcess> Start()
    {
        if (!_canBeStarted)
            throw new InvalidOperationException("This process was already started.");
        
        _startedProcess = _systemEnvironment.CreateProcess();
        //build.StartInfo.WorkingDirectory = "";
        _startedProcess.StartInfo.Arguments = _arguments;
        _startedProcess.StartInfo.FileName = _systemEnvironment.FileSystem.ResolveOvsFilePath(_exeFile, false);

        _startedProcess.StartInfo.UseShellExecute = false;
        _startedProcess.StartInfo.RedirectStandardOutput = true;
        _startedProcess.StartInfo.RedirectStandardError = true;
        _startedProcess.StartInfo.CreateNoWindow = true;
        _startedProcess.EnableRaisingEvents = true;
        // ReSharper disable once InvertIf
        if (_messageHandlers.Count > 0)
        {
            _startedProcess.OutputDataReceived += (_, _) => { };
            _startedProcess.ErrorDataReceived += (_, args) =>
            {
                if (args.Data == null) return;
                ProcessMessageAsync(args.Data);
            };
        }

        return Prelude.Try(() =>
        {
            _startedProcess.Start();
            if (_messageHandlers.Count > 0)
            {
                _startedProcess.BeginOutputReadLine();
                _startedProcess.BeginErrorReadLine();
            }
            return this;
        });
    }

    private async void ProcessMessageAsync(string? data)
    {
        await Task.Factory.StartNew(() =>
        {
            foreach (var messageHandler in _messageHandlers) messageHandler(data);

        });

    }

    public TryAsync<int> WaitForExit(bool softWait, CancellationToken cancellationToken)
    {
        return Prelude.TryAsync(async () =>
        {
            if (_startedProcess == null)
                throw new IOException("Process not started");

           
            var internalTokenSource =
                new CancellationTokenSource(Timeout.Infinite);
            if (softWait)
            {
                internalTokenSource = new CancellationTokenSource(5000);
            }
            
            var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                internalTokenSource.Token,
                cancellationToken
            );

            if (_startedProcess.HasExited) return _startedProcess.ExitCode;
            
            try
            {
                 await _startedProcess.WaitForExit(cancelSource.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_startedProcess.HasExited)
                {
                    if (softWait)
                        return 0;

                    _startedProcess.Kill();
                    throw new TimeoutException(
                        $"Process {_exeFile.Name} has not exited before timeout. Message: {ex.Message}", ex);
                }
            }

            if (!_startedProcess.HasExited)
            {
                if (softWait)
                    return 0;

                _startedProcess.Kill();
                throw new TimeoutException(
                    $"Process {_exeFile.Name} has not exited before timeout.");

            }

            return _startedProcess.ExitCode;
        });
    }

    public TryAsync<string> WaitForExitWithResponse(CancellationToken cancellationToken)
    {
        return Prelude.TryAsync(async () =>
        {
            if (_startedProcess == null)
                throw new IOException("Process not started");

            if (!_canBeRedirected)
                throw new IOException("Process was attached and output cannot be redirected.");

            try
            {
                // Read both outputs in parallel. This is necessary to avoid deadlocks.
                var outputs = await Task.WhenAll(
                    _startedProcess.StandardOutput.ReadToEndAsync(cancellationToken),
                    _startedProcess.StandardError.ReadToEndAsync(cancellationToken));

                await _startedProcess.WaitForExit(cancellationToken);

                if (_startedProcess.ExitCode == 0)
                {
                    // Only return the standard output on success as the output is processed further.
                    return outputs[0].TrimEnd();
                }
                
                var output = string.Join("\n", outputs.Where(s => !string.IsNullOrWhiteSpace(s)));
                throw new IOException($"Process {_exeFile.Name} failed with code {_startedProcess.ExitCode}. Output:\n{output}");
            }
            catch (OperationCanceledException)
            {
                _startedProcess.Kill();
                throw new TimeoutException($"Process {_exeFile.Name} has not exited before timeout.");
            }
        });
    }

    public void AddMessageHandler(Action<string?> messageHandler)
    {
        _messageHandlers.Add(messageHandler);
    }


    protected void Dispose(bool disposing)
    {
        if (!disposing) return;
        _startedProcess?.Dispose();
    }

    [ExcludeFromCodeCoverage]
    ~OVSProcess()
    {
        Dispose(false);
    }

    public TryAsync<Unit> KillAsync() => 
        Prelude.TryAsync(async () =>
        {
            if(_startedProcess is null)
                return Unit.Default;
            
            _startedProcess.Kill();
            
            // Kill() itself is async.
            using var cts = new CancellationTokenSource(5000);
            await _startedProcess.WaitForExit(cts.Token);

            return Unit.Default;
        });
    
}