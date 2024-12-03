using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dbosoft.OVN.OSCommands;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN;

/// <summary>
/// Default implementation of operating system abstraction.
/// </summary>
[ExcludeFromCodeCoverage]
[PublicAPI]
public class SystemEnvironment : ISystemEnvironment
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Creates a new environment.
    /// </summary>
    public SystemEnvironment(ILoggerFactory loggerFactory)
    {
        if (OperatingSystem.IsWindows() && (GetType() == typeof(SystemEnvironment)))
            throw new PlatformNotSupportedException("Use the Dbosoft.OVN.Windows package");

        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public virtual IProcess CreateProcess(int processId = 0)
    {
        if (processId == 0)
            return new ProcessWrapper(new Process(), _loggerFactory.CreateLogger<ProcessWrapper>());

        var process = Process.GetProcessById(processId);
        return new ProcessWrapper(process, _loggerFactory.CreateLogger<ProcessWrapper>());
    }

    /// <inheritdoc />
    public virtual IServiceManager GetServiceManager(string serviceName)
    {
        throw new PlatformNotSupportedException();
    }

    /// <inheritdoc />
    public virtual IOvsExtensionManager GetOvsExtensionManager()
    {
        return new OvsExtensionManager();
    }

    /// <inheritdoc />
    public virtual IFileSystem FileSystem => new DefaultFileSystem(GetPlatform());

    private static IEnumerable<OSPlatform> EnumeratePlatforms()
    {
        yield return OSPlatform.Windows;
        yield return OSPlatform.Linux;
        yield return OSPlatform.OSX;
        yield return OSPlatform.FreeBSD;
    }

    private static OSPlatform GetPlatform()
    {
        return EnumeratePlatforms().First(RuntimeInformation.IsOSPlatform);
    }
}
