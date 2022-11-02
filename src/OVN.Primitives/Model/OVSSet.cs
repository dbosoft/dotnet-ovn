using System.Text;
using LanguageExt;

namespace Dbosoft.OVN.Model;

public readonly record struct OVSSet<T>(Seq<T> Set) : IOVSField where T : notnull
{
    public string GetColumnValue(string columnName, bool setMode)
    {
        var space = setMode ? "=" : " ";
        var sb = new StringBuilder();
        Set.Iter(value =>
        {
            var valueString = value is string
                ? $"\"\\\"{value}\\\"\""
                : value?.ToString();

            if (valueString == null) return;
            sb.Append($"{valueString},");
        });

        return $"{columnName}{space}[{sb.ToString().TrimEnd(',')}]";
    }

    public string GetQueryString(string columnName, string option)
    {
        throw new NotSupportedException("A set cannot be used in queries.");
    }

    public static OVSSet<T> New(Seq<T> enumerable)
    {
        return new OVSSet<T>(enumerable);
    }

    public static OVSFieldMetadata Metadata(bool notEmpty = false)
    {
        return new OVSFieldMetadata(typeof(OVSSet<T>), notEmpty);
    }
}