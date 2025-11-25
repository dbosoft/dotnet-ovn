namespace Dbosoft.OVN.Model;

public readonly record struct OVSValue<T>(T Value) : IOVSField where T : notnull
{
    public string GetColumnValue(string columnName, bool setMode)
    {
        var spacer = setMode ? "=" : " ";
        return Value switch
        {
            string => $"{columnName}{spacer}\"\\\"{Value}\\\"\"",
            Guid guid => $"{columnName}{spacer}\"{guid:D}\"",
            _ => $"{columnName}{spacer}{Value}",
        };
    }

    /// <summary>
    /// Returns the column value encoded such that it can be used
    /// in update operations for a map column.
    /// </summary>
    public string GetColumnKeyValue(string columnName, string keyName)
    {
        return Value switch
        {
            string => $"{columnName}:{keyName}=\"\\\"{Value}\\\"\"",
            Guid guid => $"{columnName}:{keyName}=\"{guid:D}\"",
            _ => $"{columnName}:{keyName}={Value}",
        };
    }

    public string GetQueryString(string columnName, string option)
    {
        return Value switch
        {
            string => $"{columnName}{option}\"\\\"{Value}\\\"\"",
            Guid guid => $"{columnName}{option}\"{guid:D}\"",
            _ => $"{columnName}{option}{Value}",
        };
    }

    public static OVSValue<T> New(T value)
    {
        return new OVSValue<T>(value);
    }

    public static OVSFieldMetadata Metadata(bool notEmpty = false)
    {
        return new OVSFieldMetadata(typeof(OVSValue<T>), notEmpty);
    }
}