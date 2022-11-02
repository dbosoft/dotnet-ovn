using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedDHCPOptions : OVSEntity, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "cidr", OVSValue<string>.Metadata() },
            { "options", OVSMap<string>.Metadata() }
        };

    public string? Cidr
    {
        get => GetValue<string>("cidr");
        set => SetValue("cidr", value);
    }

    public Map<string, string> Options
    {
        get => GetMap<string>("options");
        set => SetMap("options", value);
    }

    public string? Name => ExternalIds.ContainsKey("id") ? ExternalIds["id"] : null;
}