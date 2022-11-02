namespace Dbosoft.OVN.Model;

public readonly record struct OVSValue<T>(T Value) : IOVSField where T : notnull
{
    public string GetColumnValue(string columnName, bool setMode)
    {
        var spacer = setMode ? "=" : " ";
        return Value switch
        {
            string => $"{columnName}{spacer}\"\\\"{Value}\\\"\"",
            Guid guid => $"{columnName}{spacer}\"{guid.ToString("D")}\"",
            _ => $"{columnName}{spacer}{Value}"
        };
    }

    public string GetQueryString(string columnName, string option)
    {
        return Value switch
        {
            string => $"{columnName}{option}\"\\\"{Value}\\\"\"",
            Guid guid => $"{columnName}{option}\"{guid.ToString("D")}\"",
            _ => $"{columnName}{option}{Value}"
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