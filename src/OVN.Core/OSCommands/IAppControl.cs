using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public interface IAppControl
{
    EitherAsync<Error, Unit> StopApp(CancellationToken cancellationToken = default);
    EitherAsync<Error, string> GetVersion(CancellationToken cancellationToken = default);
}