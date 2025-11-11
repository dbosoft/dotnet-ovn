using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.Model.OVN;

public record PlannedSouthboundConnection : OVSEntity, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "target", OVSValue<string>.Metadata() }
        };

    public string? Target
    {
        get => GetValue<string>("target");
        init => SetValue("target", value);
    }

    public string? Name => Target;

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(OVNSouthboundTableNames.Global, ".", "Connections");
    }
}
