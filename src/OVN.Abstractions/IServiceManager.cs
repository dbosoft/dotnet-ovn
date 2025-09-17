using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

public interface IServiceManager
{
    EitherAsync<Error, bool> ServiceExists();
    
    EitherAsync<Error, string> GetServiceCommand();

    /// <summary>
    /// Creates a new service with the given settings.
    /// </summary>
    /// <remarks>
    /// The service is not started automatically. Use
    /// <see cref="EnsureServiceStarted"/> to start it.
    /// </remarks>
    EitherAsync<Error, Unit> CreateService(
        string displayName,
        string command,
        Seq<string> dependencies,
        CancellationToken cancellationToken);
    
    EitherAsync<Error, Unit> RemoveService(CancellationToken cancellationToken);
    
    EitherAsync<Error, Unit> EnsureServiceStarted(CancellationToken cancellationToken);
    
    EitherAsync<Error, Unit> EnsureServiceStopped(CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates the executable path of the service.
    /// </summary>
    /// <remarks>
    /// The service must be stopped and restarted separately.
    /// </remarks>
    EitherAsync<Error, Unit> UpdateService(string command, CancellationToken cancellationToken);

    EitherAsync<Error, Unit> SetRecoveryOptions(
        Option<TimeSpan> firstRestartDelay,
        Option<TimeSpan> secondRestartDelay,
        Option<TimeSpan> subsequentRestartDelay,
        Option<TimeSpan> resetDelay,
        CancellationToken cancellationToken);
}
