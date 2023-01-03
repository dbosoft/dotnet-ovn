using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Nodes;

/// <summary>
/// Hosted service implementation for a OVS Node. 
/// </summary>
/// <typeparam name="TNode"></typeparam>
public class OVSNodeService<TNode> : IOVSService<TNode>, IDisposable, IAsyncDisposable
    where TNode : IOVSNode
{
    private readonly ILogger<OVSNodeService<TNode>> _logger;
    private readonly IOVSNode _ovsNode;
    private Task? _executingTask;
    private DateTime _lastResponseCheck = DateTime.MinValue;
    private CancellationTokenSource? _stoppingCts;
    private Timer? _timer;

    /// <summary>
    /// Creates a new hosted service for <typeparamref name="TNode"/>.
    /// </summary>
    /// <param name="ovsNode"></param>
    /// <param name="logger"></param>
    public OVSNodeService(
        TNode ovsNode,
        ILogger<OVSNodeService<TNode>> logger)
    {
        _logger = logger;
        _ovsNode = ovsNode;
    }


    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_timer != null)
            await _timer.DisposeAsync();
        await _ovsNode.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _timer?.Dispose();
        _ovsNode.Dispose();
    }


    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _stoppingCts = new CancellationTokenSource();
        await _ovsNode.Start(stoppingToken).IfLeft(
            l => _logger.LogError("Node service {nodeName}: Error in node startup. {error}", typeof(TNode), l));

        _timer = new Timer(FireTask, null, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));
    }

    public async Task StopAsync(bool ensureNodeStopped, CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);

        try
        {
            if (_executingTask is { IsCompleted: false })
                try
                {
                    _stoppingCts?.Cancel();
                }
                finally
                {
                    await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, stoppingToken));
                }
        }
        finally
        {
            await _ovsNode.Stop(ensureNodeStopped,stoppingToken).IfLeft(
                l => _logger.LogDebug("Node service {nodeName}: Error in node stop: {error}", typeof(TNode), l));
        }
    }

    public Task DisconnectDemons()
    {
        return _ovsNode.Disconnect().IfLeft(
            l => _logger.LogDebug("Node service {nodeName}: Error on disconnecting demons: {error}", typeof(TNode), l));
    }

    private void FireTask(object? state)
    {
        if (_executingTask == null || _executingTask.IsCompleted)
        {
            _logger.LogTrace("Running ovs node keep alive check");
            _executingTask = ExecuteNextJobAsync(_stoppingCts?.Token ?? default);
        }
        else
        {
            _logger.LogTrace("OVS keep alive check is still running. Wait for next cycle.");
        }
    }

    private async Task ExecuteNextJobAsync(CancellationToken cancellationToken)
    {
        var timeLastResponseCheck = DateTime.Now - _lastResponseCheck;
        var responseCheck = false;
        if (timeLastResponseCheck.TotalMinutes > 1)
        {
            responseCheck = true;
            _lastResponseCheck = DateTime.Now;
        }
        
        var timeOutCts = new CancellationTokenSource(5000);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeOutCts.Token);

        await _ovsNode.EnsureAlive(responseCheck, cts.Token).IfLeft(
            l => _logger.LogDebug("Node service {nodeName}: Error in check alive: {error}", typeof(TNode), l));
    }
}