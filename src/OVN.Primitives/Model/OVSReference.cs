using System.Text;
using LanguageExt;

namespace Dbosoft.OVN.Model;

public readonly record struct OVSReference(Seq<Guid> Set) : IOVSField
{
    public string GetColumnValue(string columnName, bool setMode)
    {
        var sb = new StringBuilder();
        Set.Iter(value =>
        {
            if (value != Guid.Empty)
                sb.Append((string?)$"{value:D},");
        });

        var spacer = setMode ? "=" : " ";
        return $"{columnName}{spacer}[{sb.ToString().TrimEnd(',')}]";
    }

    public string GetQueryString(string columnName, string option)
    {
        throw new NotSupportedException("A set cannot be used in queries.");
    }

    public static OVSReference New(Seq<Guid> enumerable)
    {
        return new OVSReference(enumerable);
    }

    public static OVSFieldMetadata Metadata(bool notEmpty = false)
    {
        return new OVSFieldMetadata(typeof(OVSReference), notEmpty);
    }
}