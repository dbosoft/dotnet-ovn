using LanguageExt;

namespace Dbosoft.OVN.Model;

public interface IHasOVSReferences<TEntity> where TEntity : OVSTableRecord
{
    public Guid Id { get; }

    public Seq<Guid> GetOvsReferences();
}
