using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record DHCPOptions : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "cidr", OVSValue<string>.Metadata() },
            { "options", OVSMap<string>.Metadata() }
        };

    public string? Cidr => GetValue<string>("cidr");
    public Map<string, string> Options => GetMap<string>("options");
    public string? Name => ExternalIds.ContainsKey("id") ? ExternalIds["id"] : null;
}