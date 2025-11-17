using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.Model;

public abstract record OVSGlobalTableRecord : OVSTableRecord
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "ssl", OVSReference.Metadata() },
        };

    public Seq<Guid> Ssl => GetReference("ssl");
}
