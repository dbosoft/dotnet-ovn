using LanguageExt;

namespace Dbosoft.OVN.Model;

public readonly record struct OVSParentReference(
    string TableName, Option<string> RowId, string RefColumn);