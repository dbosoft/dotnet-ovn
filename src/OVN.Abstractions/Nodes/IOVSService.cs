using Dbosoft.OVN.Nodes;

namespace Dbosoft.OVN;

public interface IOVSService<TNode> where TNode: IOVSNode
{

    /// <summary>
    /// Start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop the service
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    Task StopAsync(bool ensureNodeStopped, CancellationToken cancellationToken);
    
    /// <summary>
    /// Disconnects all running demons from the node and keep them running.
    /// </summary>
    /// <returns></returns>
    Task DisconnectDemons();
}