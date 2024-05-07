using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedDnsRecords : OVSEntity, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "records", OVSMap<string>.Metadata() },
            { "options", OVSMap<string>.Metadata() },
        };
    

    public Map<string, string> Records
    {
        get => GetMap<string>("records");
        set => SetMap("records", value);
    }

    public Map<string, string> Options
    {
        get => GetMap<string>("options");
        set => SetMap("options", value);
    }

    public string? Name => ExternalIds.ContainsKey("id") ? ExternalIds["id"] : null;
}