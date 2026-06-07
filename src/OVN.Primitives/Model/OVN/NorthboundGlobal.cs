using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record NorthboundGlobal : OVSGlobalTableRecord, IOVSEntityWithName, IHasOVSReferences<NorthboundConnection>, IHasOVSReferences<NorthboundSsl>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSGlobalTableRecord.Columns)
        {
            { "connections", OVSReference.Metadata() },
        };

    public Seq<Guid> Connections => GetReference("connections");

    public string? Name => ".";

    Seq<Guid> IHasOVSReferences<NorthboundConnection>.GetOvsReferences() => Connections;

    Seq<Guid> IHasOVSReferences<NorthboundSsl>.GetOvsReferences() => Ssl;
}
