using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundGlobal : OVSGlobalTableRecord, IOVSEntityWithName, IHasOVSReferences<SouthboundConnection>, IHasOVSReferences<SouthboundSsl>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSGlobalTableRecord.Columns)
        {
            { "connections", OVSReference.Metadata() },
        };

    public Seq<Guid> Connections => GetReference("connections");

    public string? Name => ".";

    Seq<Guid> IHasOVSReferences<SouthboundConnection>.GetOvsReferences() => Connections;

    Seq<Guid> IHasOVSReferences<SouthboundSsl>.GetOvsReferences() => Ssl;
}
