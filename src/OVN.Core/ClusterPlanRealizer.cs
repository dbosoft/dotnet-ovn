using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN;

public class ClusterPlanRealizer
{
    private readonly ClusterPlanNorthboundRealizer _northboundRealizer;
    private readonly ClusterPlanSouthboundRealizer _southboundRealizer;
    
    public ClusterPlanRealizer(
        IOVSDBTool northboundOvnDbTool,
        IOVSDBTool southboundOvnDbTool,
        ILogger logger)
    {
        _northboundRealizer = new ClusterPlanNorthboundRealizer(northboundOvnDbTool, logger);
        _southboundRealizer = new ClusterPlanSouthboundRealizer(southboundOvnDbTool, logger);
    }

    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from _1 in _northboundRealizer.ApplyClusterPlan(clusterPlan, cancellationToken)
        from _2 in _southboundRealizer.ApplyClusterPlan(clusterPlan, cancellationToken)
        select clusterPlan;
}
