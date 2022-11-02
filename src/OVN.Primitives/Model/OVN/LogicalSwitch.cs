using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record LogicalSwitch : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "ports", OVSReference.Metadata() },
            { "dns_records", OVSSet<Guid>.Metadata() }
        };

    public Seq<Guid> Ports => GetReference("ports");

    public string? Name => GetValue<string>("name");
    
    public Seq<Guid> DnsRecords => GetSet<Guid>("dns_records");
}