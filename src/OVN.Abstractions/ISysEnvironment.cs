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
    /// <param name="processId"></param>
    /// <returns></returns>
    IProcess CreateProcess(int processId = 0);

    /// <summary>
    /// Gets a service manager for the service name
    /// </summary>
    /// <param name="serviceName">name of service</param>
    /// <returns></returns>
    IServiceManager GetServiceManager(string serviceName);

    IOvsExtensionManager GetOvsExtensionManager();
}