using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record DnsRecords : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "records", OVSMap<string>.Metadata() }
        };
    
    public Map<string, string> Records => GetMap<string>("records");
    public string? Name => ExternalIds.ContainsKey("id") ? ExternalIds["id"] : null;
}