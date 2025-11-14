using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;

namespace Dbosoft.OVN.Model;

public static class OVSEntityMetadata
{
    private static Map<string, Func<IDictionary<string, OVSFieldMetadata>>> _lookup;

    public static IDictionary<string, OVSFieldMetadata> Get(Type ovsType)
    {
        (_lookup, var value) = _lookup.FindOrAdd(
            ovsType.FullName ??
            throw new InvalidCastException($"Type {ovsType} cannot be used as OVS type."),
            () => MakeDelegate(ovsType));
        return value();
    }

    private static Func<IDictionary<string, OVSFieldMetadata>> MakeDelegate(Type ovsType)
    {
        var field = ovsType.GetField("Columns", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field == null)
            throw new InvalidOperationException(
                $"Failed to access column metadata of entity {ovsType}");

        var fieldExpression = Expression.Field(null, field);
        var lambda = Expression.Lambda<Func<IDictionary<string, OVSFieldMetadata>>>(fieldExpression);

        return lambda.Compile();
    }
}