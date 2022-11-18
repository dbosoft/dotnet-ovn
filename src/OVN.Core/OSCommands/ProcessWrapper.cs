using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.OSCommands;

/// <summary>
///     Wrapper for <see cref="System.Diagnostics.Process" />
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessWrapper : IProcess
{
    private readonly ILogger _logger;
    private readonly Process _process;
    
    /// <summary>
    /// creates a new process wrapper from given process. 
    /// </summary>
    /// <param name="process"></param>
    /// <param name="logger"></param>
    public ProcessWrapper(Process process, ILogger logger)
    {
        _process = process;
        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived+=OnErrorDataReceived;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _process.Dispose();
    }

    /// <inheritdoc />
    public ProcessStartInfo StartInfo => _process.StartInfo;

    /// <inheritdoc />
    public bool EnableRaisingEvents
    {
        get => _process.EnableRaisingEvents;
        set => _process.EnableRaisingEvents = value;
    }

    /// <inheritdoc />
    public string ProcessName => _process.ProcessName;

    /// <inheritdoc />
    public int ExitCode
    {
        get
        {
            try
            {
                return _process.ExitCode;
            }
            catch (Exception)
            {
                return int.MinValue;
            }
            
        }
    }

    /// <inheritdoc />
    public event DataReceivedEventHandler? OutputDataReceived;

    /// <inheritdoc />
    public event DataReceivedEventHandler? ErrorDataReceived;

    private void OnOutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        OutputDataReceived?.Invoke(sender,new DataReceivedEventArgs(e.Data));
    }
    
    private void OnErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        ErrorDataReceived?.Invoke(sender,new DataReceivedEventArgs(e.Data));
    }
    

    /// <inheritdoc />
    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public void BeginOutputReadLine()
    {
        _process.BeginOutputReadLine();
    }

    /// <inheritdoc />
    public void BeginErrorReadLine()
    {
        _process.BeginErrorReadLine();
    }

    /// <inheritdoc />
    public Task WaitForExit(CancellationToken cancellationToken)
    {
        return _process.WaitForExitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task WaitForExit()
    {
        return _process.WaitForExitAsync();
    }

    /// <inheritdoc />
    public void Start()
    {
        _logger.LogTrace("Starting process '{name}' with arguments '{arguments}'",
            _process.StartInfo.FileName, _process.StartInfo.Arguments);
        _process.Start();
    }

    /// <inheritdoc />
    public void Kill()
    {
        if (_process.HasExited)
            return;

        try
        {
            _process.Kill();
        }
        catch
        {
            //ignored
        }
    }
}