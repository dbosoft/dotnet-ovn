using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model;

[PublicAPI]
public record OVSEntity
{
    public static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>
        {
            { "external_ids", OVSMap<string>.Metadata() }
        };

    private Map<string, IOVSField> Values { get; set; }


    public Map<string, string> ExternalIds
    {
        get => GetMap<string>("external_ids");
        init => SetMap("external_ids", value);
    }

    public Map<string, IOVSField> ToMap()
    {
        return Values;
    }

    protected T? GetValue<T>(string propertyName) where T : notnull
    {
        return !Values.ContainsKey(propertyName)
            ? default
            : ((OVSValue<T>)Values[propertyName]).Value;
    }

    protected Seq<T> GetSet<T>(string propertyName) where T : notnull
    {
        return !Values.ContainsKey(propertyName)
            ? default
            : ((OVSSet<T>)Values[propertyName]).Set;
    }

    protected Seq<Guid> GetReference(string propertyName)
    {
        return !Values.ContainsKey(propertyName)
            ? default
            : ((OVSReference)Values[propertyName]).Set;
    }

    protected Map<string, T> GetMap<T>(string propertyName)
    {
        return !Values.ContainsKey(propertyName)
            ? default
            : ((OVSMap<T>)Values[propertyName]).Map;
    }

    protected void SetValue<T>(string propertyName, T? value)
    {
        if (Values.ContainsKey(propertyName)) Values = Values.Remove(propertyName);

        if (value != null)
            Values = Values.Add(propertyName, OVSValue<T>.New(value));
    }

    protected void SetSet<T>(string propertyName, Seq<T> value) where T : notnull
    {
        if (Values.ContainsKey(propertyName)) Values = Values.Remove(propertyName);

        if (value != default)
            Values = Values.Add(propertyName, OVSSet<T>.New(value));
    }

    protected void SetMap<T>(string propertyName, Map<string, T> value) where T : notnull
    {
        if (Values.ContainsKey(propertyName)) Values = Values.Remove(propertyName);

        if (value != default)
            Values = Values.Add(propertyName, OVSMap<T>.New(value));
    }

    public static T FromValueMap<T>(Map<string, IOVSField> values) where T : OVSEntity, new()
    {
        var record = new T
        {
            Values = values
        };

        return record;
    }
}