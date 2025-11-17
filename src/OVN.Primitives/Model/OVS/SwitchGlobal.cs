using LanguageExt;

namespace Dbosoft.OVN.Model.OVS;

public record SwitchGlobal : OVSGlobalTableRecord, IOVSEntityWithName, IHasOVSReferences<SwitchSsl>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "ssl", OVSReference.Metadata() },
        };

    public string? Name => ".";

    Seq<Guid> IHasOVSReferences<SwitchSsl>.GetOvsReferences() => Ssl;
}
