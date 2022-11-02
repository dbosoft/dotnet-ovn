using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
public class SystemEnvironment : ISysEnvironment
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// creates a new environment.
    /// </summary>
    /// <param name="loggerFactory">Logger factory</param>
    public SystemEnvironment(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public virtual IProcess CreateProcess()
    {
        return new ProcessWrapper(new Process(), _loggerFactory.CreateLogger<ProcessWrapper>());
    }

    public IServiceManager GetServiceManager(string serviceName)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsServiceManager(serviceName, this);

        throw new PlatformNotSupportedException();
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