using JetBrains.Annotations;

namespace Dbosoft.OVN.Model.OVN;

public record PlannedNATRule(string RouterName) : OVSTableRecord, IHasParentReference, IOVSEntityWithName
{
    [UsedImplicitly] public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "type", OVSValue<string>.Metadata() },
            { "external_ip", OVSValue<string>.Metadata() },
            { "external_mac", OVSValue<string>.Metadata() },
            { "logical_ip", OVSValue<string>.Metadata() }
        };

    public string? Type
    {
        get => GetValue<string>("type");
        init => SetValue("type", value);
    }

    public string? ExternalIP
    {
        get => GetValue<string>("external_ip");
        init => SetValue("external_ip", value);
    }

    public string? ExternalMAC
    {
        get => GetValue<string>("external_mac");
        init => SetValue("external_mac", value);
    }

    public string? LogicalIP
    {
        get => GetValue<string>("logical_ip");
        init => SetValue("logical_ip", value);
    }

    public OVSParentReference? GetParentReference()
    {
        return new OVSParentReference(OVNTableNames.LogicalRouter,
            RouterName, "nat");
    }


    public string Name =>
        $"router:{RouterName}, type:{Type}, externalIP:{ExternalIP}, externalMac: {ExternalMAC}, logicalIP: {LogicalIP}";
}