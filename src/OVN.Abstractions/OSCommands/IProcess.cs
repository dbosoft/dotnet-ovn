using System.Diagnostics;
using JetBrains.Annotations;

namespace Dbosoft.OVN.OSCommands;

/// <summary>
///     Wrapper Interface for <see cref="System.Diagnostics.Process" />
/// </summary>
[PublicAPI]
public interface IProcess : IDisposable
{
    
    /// <summary>
    /// See <see cref="Process.StartInfo"/>
    /// </summary>
    ProcessStartInfo StartInfo { get; }
    /// <summary>
    /// See <see cref="Process.EnableRaisingEvents"/>
    /// </summary>
    bool EnableRaisingEvents { get; set; }
    
    /// <summary>
    /// See <see cref="Process.ProcessName"/>
    /// </summary>
    public string ProcessName { get; }
    
    /// <summary>
    /// See <see cref="Process.ExitCode"/>
    /// </summary>
    public int ExitCode { get; }
    
    /// <summary>
    /// See <see cref="Process.HasExited"/>
    /// </summary>
    bool HasExited { get; }
    
    /// <summary>
    /// See <see cref="Process.OutputDataReceived"/>
    /// </summary>
    public event DataReceivedEventHandler? OutputDataReceived;
    
    /// <summary>
    /// See <see cref="Process.ErrorDataReceived"/>
    /// </summary>
    public event DataReceivedEventHandler? ErrorDataReceived;
    
    /// <summary>
    /// See <see cref="Process.BeginOutputReadLine"/>
    /// </summary>
    public void BeginOutputReadLine();
    
    /// <summary>
    /// See <see cref="Process.BeginErrorReadLine"/>
    /// </summary>
    public void BeginErrorReadLine();
    
    /// <summary>
    /// See <see cref="Process.WaitForExit()"/>
    /// </summary>
    public Task WaitForExit(CancellationToken cancellationToken);
    
    /// <summary>
    /// See <see cref="Process.WaitForExit()"/>
    /// </summary>
    public Task WaitForExit();
    
    /// <summary>
    /// See <see cref="Process.Start()"/>
    /// </summary>
    public void Start();

    /// <summary>
    /// See <see cref="Process.Kill()"/>
    /// </summary>
    void Kill();
}