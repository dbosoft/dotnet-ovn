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
    private readonly ISysEnvironment _syEnv;
    private IProcess? _startedProcess;

    public OVSProcess(ISysEnvironment syEnv, OvsFile exeFile, string arguments = "")
    {
        _syEnv = syEnv;
        _exeFile = exeFile;
        _arguments = arguments;
    }

    public bool IsRunning => _startedProcess is { HasExited: false };

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Try<OVSProcess> Start()
    {
        _startedProcess = _syEnv.CreateProcess();
        //build.StartInfo.WorkingDirectory = "";
        _startedProcess.StartInfo.Arguments = _arguments;
        _startedProcess.StartInfo.FileName = _syEnv.FileSystem.ResolveOvsFilePath(_exeFile, false);

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
                foreach (var messageHandler in _messageHandlers) messageHandler(args.Data);
            };
        }

        return Prelude.Try(() =>
        {
            _startedProcess.Start();
            _startedProcess.BeginOutputReadLine();
            _startedProcess.BeginErrorReadLine();
            return this;
        });
    }

    public TryAsync<string> WaitForExit(CancellationToken cancellationToken)
    {
        return Prelude.TryAsync(async () =>
        {
            if (_startedProcess == null)
                throw new IOException("Process not started");

            var outputBuilder = new StringBuilder();
            _startedProcess.OutputDataReceived += (_, o) => { outputBuilder.Append(o.Data); };
            _startedProcess.ErrorDataReceived += (_, o) => { outputBuilder.Append(o.Data); };

            try
            {
                await _startedProcess.WaitForExit(cancellationToken);
            }
            catch (Exception ex)
            {
                _startedProcess.Kill();
                throw new TimeoutException(
                    $"Process {_exeFile.Name} has not exited before timeout. \nOutput: {outputBuilder}", ex);
            }

            // ReSharper disable once MethodSupportsCancellation
            //this is required here to force output to be read until end
            await _startedProcess.WaitForExit();

            var output = outputBuilder.ToString();

            if (_startedProcess.ExitCode != 0)
                throw new IOException(
                    $"Process {_exeFile.Name} failed with code {_startedProcess.ExitCode}. \nOutput: {output}");

            return output.TrimEnd().TrimEnd('\n', '\r');
        });
    }


    public void AddMessageHandler(Action<string?> messageHandler)
    {
        _messageHandlers.Add(messageHandler);
    }


    protected void Dispose(bool disposing)
    {
        if (!disposing) return;
        _startedProcess?.Kill();
        _startedProcess?.Dispose();
    }

    [ExcludeFromCodeCoverage]
    ~OVSProcess()
    {
        Dispose(false);
    }

    public Try<Unit> Kill()
    {
        return Prelude.Try(() =>
        {
            _startedProcess?.Kill();
            return Unit.Default;
        });
    }
}