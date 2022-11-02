namespace Dbosoft.OVN.Model;

public readonly record struct OVSParentReference(
    string TableName, string RowId, string RefColumn);