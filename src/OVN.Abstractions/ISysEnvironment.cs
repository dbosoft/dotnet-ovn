using Dbosoft.OVN.OSCommands;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

/// <summary>
/// Abstraction of operating system.
/// </summary>
public interface ISysEnvironment
{
    /// <summary>
    /// File system
    /// </summary>
    IFileSystem FileSystem { get; }
    
    /// <summary>
    /// creates a new process
    /// </summary>
    /// <returns></returns>
    IProcess CreateProcess();

    IServiceManager GetServiceManager(string serviceName);
    
}

public interface IServiceManager
{
    EitherAsync<Error, bool> ServiceExists();
    EitherAsync<Error, string> GetServiceCommand();
    EitherAsync<Error, Unit> CreateService(string displayName, string command, CancellationToken cancellationToken);
    EitherAsync<Error, Unit> RemoveService(CancellationToken cancellationToken);
    EitherAsync<Error, Unit> EnsureServiceStarted(CancellationToken cancellationToken);
    EitherAsync<Error, Unit> EnsureServiceStopped(CancellationToken cancellationToken);
    EitherAsync<Error, Unit> UpdateService(string command, CancellationToken cancellationToken);
}