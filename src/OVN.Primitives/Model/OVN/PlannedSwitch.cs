using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedSwitch : OVSEntity, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "dns_records", OVSSet<Guid>.Metadata() }
        };

    public string? Name
    {
        get => GetValue<string>("name");
        init => SetValue("name", value);
    }
    
    public Seq<Guid> DnsRecords
    {
        get => GetSet<Guid>("dns_records");
        init => SetSet("dns_records", value);
    }
}