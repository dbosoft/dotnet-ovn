using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.Nodes;

public interface IOVSNode :  IDisposable, IAsyncDisposable
{
    EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default);
    EitherAsync<Error, Unit> Stop(bool ensureNodeStopped, CancellationToken cancellationToken = default);

    EitherAsync<Error, Unit> EnsureAlive(bool checkResponse,
        CancellationToken cancellationToken = default);
    
    EitherAsync<Error, Unit> Disconnect();

}