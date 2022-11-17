using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

/// <summary>
/// Hosted service implementation for a OVS Node. 
/// </summary>
/// <typeparam name="TNode"></typeparam>
public class OVSNodeHostedService<TNode> : IHostedService
    where TNode : IOVSNode
{
    private readonly IOVSService<TNode> _ovsNodeService;

    /// <summary>
    /// Creates a new hosted service for <typeparamref name="TNode"/>.
    /// </summary>
    /// <param name="ovsNodeService"></param>
    /// <param name="logger"></param>
    public OVSNodeHostedService(
        IOVSService<TNode> ovsNodeService)
    {
        _ovsNodeService = ovsNodeService;
    }
    

    /// <inheritdoc />
    public Task StartAsync(CancellationToken stoppingToken)
    {
        return _ovsNodeService.StartAsync(stoppingToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken stoppingToken)
    {
        return _ovsNodeService.StopAsync(false,stoppingToken);
    }

   
}