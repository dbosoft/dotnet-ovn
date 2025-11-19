using LanguageExt;

namespace Dbosoft.OVN.Model;

public static class OVSTableRecordExtensions
{
    public static Seq<TRecord> AddParentReferences<TRecord, TParent>(
        this Seq<TRecord> records,
        Seq<TParent> parents)
        where TRecord : OVSTableRecord, IHasParentReference, new()
        where TParent : OVSTableRecord, IHasOVSReferences<TRecord> =>
        AddParentReferences(
            records,
            parents.SelectMany(
                    p => p.GetOvsReferences(),
                    (parent, childId) => (childId, parent.Id))
                .ToHashMap());

    private static Seq<TRecord> AddParentReferences<TRecord>(
        this Seq<TRecord> records,
        HashMap<Guid, Guid> parentMap)
        where TRecord : OVSTableRecord, IHasParentReference, new() =>
        records.Map(r => AddParentReference(r, parentMap));

    public static TRecord AddParentReference<TRecord, TParent>(
        TRecord record,
        Seq<TParent> parents)
        where TRecord : OVSTableRecord, IHasParentReference, new()
        where TParent : OVSTableRecord, IHasOVSReferences<TRecord> =>
        AddParentReference(
            record,
            parents.SelectMany(
                    p => p.GetOvsReferences(),
                    (parent, childId) => (childId, parent.Id))
                .ToHashMap());

    private static TRecord AddParentReference<TRecord>(
        TRecord record,
        HashMap<Guid, Guid> parentMap)
        where TRecord : OVSTableRecord, IHasParentReference, new() =>
        parentMap.Find(record.Id).Match(
            Some: parentId => OVSEntity.FromValueMap<TRecord>(
                record.ToMap().Add("__parentId", OVSValue<Guid>.New(parentId))),
            None: () => record);
}
