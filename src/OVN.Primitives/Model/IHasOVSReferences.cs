using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;

namespace Dbosoft.OVN.Model;

public interface IHasOVSReferences<TEntity> where TEntity : OVSTableRecord
{
    public Guid Id { get; }

    public Seq<Guid> GetOvsReferences();
}
