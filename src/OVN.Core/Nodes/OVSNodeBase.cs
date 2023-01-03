using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.Nodes;

[PublicAPI]
public abstract class OVSNodeBase : IOVSNode
{
   
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var cts = new CancellationTokenSource(5000);
        await Stop(false,cts.Token);

        Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public abstract EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default);
    public abstract EitherAsync<Error, Unit> Stop(bool ensureNodeStopped, CancellationToken cancellationToken = default);

    
    protected virtual void Dispose(bool disposing)
    {
        
    }

    public virtual EitherAsync<Error, Unit> EnsureAlive(bool checkResponse,
        CancellationToken cancellationToken = default)
    {
        return Unit.Default;
    }

    public abstract EitherAsync<Error, Unit> Disconnect();
}