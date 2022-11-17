using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOvsNode<TNode>(this IServiceCollection services)
        where TNode: class, IOVSNode
    {
        services.AddSingleton<TNode>();
        services.AddSingleton<IOVSService<TNode>, OVSNodeService<TNode>>();

        return services;
    }
    
    public static IServiceCollection AddHostedNode<TNode>(this IServiceCollection services)
        where TNode: class, IOVSNode
    {
        AddOvsNode<TNode>(services);
        services.AddHostedService<OVSNodeHostedService<TNode>>();

        return services;
    }
}