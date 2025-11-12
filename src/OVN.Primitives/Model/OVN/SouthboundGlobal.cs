using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundGlobal : OVSTableRecord, IOVSEntityWithName, IHasOVSReferences<SouthboundConnection>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "connections", OVSReference.Metadata() },
            //{ "ssl", OVSReference.Metadata() }
        };

    public Seq<Guid> Connections => GetReference("connections");

    //public Seq<Guid> Ssl => GetReference("ssl");

    public string? Name => ".";

    Seq<Guid> IHasOVSReferences<SouthboundConnection>.GetOvsReferences() => Connections;
}
