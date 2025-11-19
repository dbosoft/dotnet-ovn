using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record LogicalRouter
    : OVSTableRecord,
        IOVSEntityWithName,
        IHasOVSReferences<LogicalRouterPort>,
        IHasOVSReferences<LogicalRouterStaticRoute>,
        IHasOVSReferences<NATRule>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "ports", OVSReference.Metadata() },
            { "static_routes", OVSReference.Metadata() },
            { "nat", OVSReference.Metadata() }
        };
    
    public string? Name => GetValue<string>("name");

    public Seq<Guid> Ports => GetReference("ports");

    public Seq<Guid> StaticRoutes => GetReference("static_routes");

    public Seq<Guid> NATRules => GetReference("nat");

    Seq<Guid> IHasOVSReferences<LogicalRouterPort>.GetOvsReferences() => Ports;

    Seq<Guid> IHasOVSReferences<LogicalRouterStaticRoute>.GetOvsReferences() => StaticRoutes;
    
    Seq<Guid> IHasOVSReferences<NATRule>.GetOvsReferences() => NATRules;
}
