using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN;

public class ClusterPlanRealizer
{
    private readonly ClusterPlanNorthboundRealizer _northboundRealizer;
    private readonly ClusterPlanSouthboundRealizer _southboundRealizer;
    
    public ClusterPlanRealizer(
        ISystemEnvironment systemEnvironment,
        IOVSDBTool northboundOvnDbTool,
        IOVSDBTool southboundOvnDbTool)
    {
        _northboundRealizer = new ClusterPlanNorthboundRealizer(systemEnvironment, northboundOvnDbTool);
        _southboundRealizer = new ClusterPlanSouthboundRealizer(systemEnvironment, southboundOvnDbTool);
    }

    public EitherAsync<Error, ClusterPlan> ApplyClusterPlan(
        ClusterPlan clusterPlan,
        CancellationToken cancellationToken = default) =>
        from _1 in _northboundRealizer.ApplyClusterPlan(clusterPlan, cancellationToken)
        from _2 in _southboundRealizer.ApplyClusterPlan(clusterPlan, cancellationToken)
        select clusterPlan;
}
