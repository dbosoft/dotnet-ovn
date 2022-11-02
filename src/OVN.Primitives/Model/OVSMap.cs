using System.Text;
using LanguageExt;

namespace Dbosoft.OVN.Model;

public readonly record struct OVSMap<T>(Map<string, T> Map) : 
    IOVSField where T : notnull
{
    public string GetColumnValue(string columnName, bool setMode)
    {
        var spacer = setMode ? "=" : " ";
        var sb = new StringBuilder();
        Map.Iter((key, value) =>
        {
            var valueString = value is string
                ? $"\"\\\"{value}\\\"\""
                : value?.ToString();

            if (valueString == null) return;
            sb.Append($"{key}={valueString},");
        });

        return $"{columnName}{spacer}{{{sb.ToString().TrimEnd(',')}}}";
    }

    public string GetQueryString(string columnName, string option)
    {
        var sb = new StringBuilder();
        Map.Iter((key, value) =>
        {
            var valueString = value is string
                ? $"\"{value}\""
                : value?.ToString();

            if (valueString == null) return;
            sb.Append($"{columnName}:{key}{option}{valueString} ");
        });

        return sb.ToString().TrimEnd();
    }

    public static OVSMap<T> New(Map<string, T> map)
    {
        return new OVSMap<T>(map);
    }

    public static OVSFieldMetadata Metadata(bool notEmpty = false)
    {
        return new OVSFieldMetadata(typeof(OVSMap<T>), notEmpty);
    }
}