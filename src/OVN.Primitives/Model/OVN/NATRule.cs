using JetBrains.Annotations;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record NATRule : OVSTableRecord, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "type", OVSValue<string>.Metadata() },
            { "external_ip", OVSValue<string>.Metadata() },
            { "external_mac", OVSValue<string>.Metadata() },
            { "logical_ip", OVSValue<string>.Metadata() },
            { "logical_port", OVSValue<string>.Metadata() }
        };

    public string? Type => GetValue<string>("type");
    public string? ExternalIP => GetValue<string>("external_ip");
    public string? ExternalMAC => GetValue<string>("external_mac");
    public string? LogicalIP => GetValue<string>("logical_ip");
    public string? LogicalPort => GetValue<string>("logical_port");

    private Guid? ParentId => GetValue<Guid>("__parentId");

    private string? RouterName => ExternalIds.ContainsKey("router_name") ? ExternalIds["router_name"] : null;

    public OVSParentReference? GetParentReference()
    {
        if (!ParentId.HasValue) return null;

        return new OVSParentReference(OVNTableNames.LogicalRouter,
            ParentId.Value.ToString("D"), "nat");
    }

    public string Name =>
        $"router:{RouterName}, type:{Type}, externalIP:{ExternalIP}, externalMac: {ExternalMAC}, logicalIP: {LogicalIP}, logicalPort: {LogicalPort}";
}