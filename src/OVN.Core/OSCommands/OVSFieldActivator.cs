using System.ComponentModel;
using System.Text.Json;
using Dbosoft.OVN.Model;
using LanguageExt;

namespace Dbosoft.OVN.OSCommands;

internal static class OVSFieldActivator
{
    public static IOVSField? JsonElementToOVSField(string context, Type type, JsonElement jsonElement)
    {
        var valueType = type.GenericTypeArguments.Any()
            ? type.GenericTypeArguments.First()
            : typeof(Guid);

        var converterType = typeof(TypedConverter<>).MakeGenericType(valueType);

        if (Activator.CreateInstance(converterType) is not IConverter converter)
            throw new InvalidOperationException(
                $"Failed to create internal converter for type {type.FullName})");

        //first case: jsonElement is not a array, so it can only be a value record or invalid

        var ovsFieldType = converter.GetOVSFieldType(type);

        if (jsonElement.ValueKind != JsonValueKind.Array)
            return ovsFieldType switch
            {
                OVSFieldType.Map => throw new InvalidOperationException(
                    $"{context}: cannot convert a json element of type {jsonElement.ValueKind} to a ovs set."),
                OVSFieldType.Value => converter.DeserializeValue(jsonElement, ""),
                OVSFieldType.Set => converter.DeserializeSet(jsonElement, "set"),
                _ => null
            };

        var valueObjects = jsonElement.Deserialize<JsonElement[]>();
        if (valueObjects is not { Length: 2 })
            throw new InvalidOperationException(
                $"{context}: Unexpected json structure: array {jsonElement} should contain two entries");

        var typeField = valueObjects[0].Deserialize<string>() ?? "";
        var valueField = valueObjects[1];

        IOVSField? value;
        switch (typeField)
        {
            case "map":
                if (ovsFieldType != OVSFieldType.Map)
                    throw new InvalidOperationException(
                        $"{context}: cannot convert a map to ovs type {ovsFieldType} ");
                value = converter.DeserializeMap(valueField);
                break;
            case "set":
                switch (ovsFieldType)
                {
                    case OVSFieldType.Value:
                        value = converter.DeserializeValue(valueField, typeField);
                        break;
                    case OVSFieldType.Set:
                        value = converter.DeserializeSet(valueField, typeField);
                        break;
                    case OVSFieldType.Map:
                    default:
                        throw new InvalidOperationException(
                            $"{context}: cannot convert a set to ovs type {ovsFieldType} ");
                
                }
                break;
            default:
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                value = ovsFieldType switch
                {
                    OVSFieldType.Value => converter.DeserializeValue(valueField, typeField),
                    OVSFieldType.Set => converter.DeserializeSet(valueField, typeField),
                    _ => throw new InvalidOperationException(
                        $"{context}: cannot convert element {valueField} to ovs type {ovsFieldType}")
                };
                break;
        }

        if (type == typeof(OVSReference) && value is OVSSet<Guid> stringSet) value = OVSReference.New(stringSet.Set);

        return value;
    }

    private enum OVSFieldType
    {
        Value,
        Set,
        Map
    }

    private interface IConverter
    {
        OVSFieldType GetOVSFieldType(Type type);
        IOVSField? DeserializeValue(JsonElement element, string typeName);
        IOVSField? DeserializeMap(JsonElement mapElement);
        IOVSField? DeserializeSet(JsonElement setElement, string typeName);
    }

    private class TypedConverter<T> : IConverter where T : notnull
    {
        public OVSFieldType GetOVSFieldType(Type type)
        {
            if (type == typeof(OVSValue<T>))
                return OVSFieldType.Value;

            if (type == typeof(OVSMap<T>))
                return OVSFieldType.Map;

            if (type == typeof(OVSSet<T>))
                return OVSFieldType.Set;

            if (type == typeof(OVSReference))
                return OVSFieldType.Set;

            throw new InvalidOperationException($"Invalid type {type}. Type has to be a OVS field");
        }


        public IOVSField? DeserializeValue(JsonElement element, string typeName)
        {
            while (true) // will only loop in case a array is found
            {
                object? objectValue = null;
                switch (element.ValueKind)
                {
                    case JsonValueKind.Undefined:
                        goto default;
                    case JsonValueKind.Object:
                        goto default;
                    case JsonValueKind.Array:
                    {
                        // a optional value may be send as array
                        var setRows = element.Deserialize<IEnumerable<JsonElement>>()
                            ?.ToArray();

                        if (setRows == null || setRows.Length == 0) return null;
                        element = setRows[0];
                        continue;
                    }
                    case JsonValueKind.String:
                        var jsonStringValue = element.Deserialize<string>();
                        if (jsonStringValue != null)
                        {
                            objectValue = TypeDescriptor.GetConverter(typeof(T))
                                .ConvertFromInvariantString(jsonStringValue);
                        }

                        break;
                    case JsonValueKind.Number:
                        var jsonDoubleValue = element.Deserialize<double>();

                        if(typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(short) || typeof(T) == typeof(byte))
                            objectValue = TypeDescriptor.GetConverter(typeof(T))
                                .ConvertFrom(jsonDoubleValue.ToString("F0"));
                        else
                            objectValue = TypeDescriptor.GetConverter(typeof(T))
                                .ConvertFrom(jsonDoubleValue);

                        break;
                    case JsonValueKind.True:
                        var jsonTrueValue = element.Deserialize<bool>();
                        objectValue = TypeDescriptor.GetConverter(typeof(T))
                            .ConvertFrom(jsonTrueValue);
                        break;
                    case JsonValueKind.False:
                        var jsonFalseValue = element.Deserialize<bool>();
                        objectValue = TypeDescriptor.GetConverter(typeof(T))
                            .ConvertFrom(jsonFalseValue);
                        break;
                    case JsonValueKind.Null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(element));
                }

                return objectValue == null
                    ? null
                    : OVSValue<T>.New((T)objectValue);
            }
        }

        public IOVSField? DeserializeMap(JsonElement mapElement)
        {
            //currently we assume that all maps contain string value
            //so we return always return a OVSMap<string>
            var mapFields = mapElement.Deserialize<IEnumerable<JsonElement>>();

            if (mapFields == null)
                return null;


            var result = new Dictionary<string, T>();
            foreach (var mapField in mapFields)
            {
                var valueStrings = mapField.Deserialize<JsonElement[]>();
                if (valueStrings is not { Length: 2 })
                    continue;

                var key = valueStrings[0].GetString();
                var value = valueStrings[1].Deserialize<T>();

                if (key != null && value != null)
                    result.Add(key, value);
            }

            return result.Count == 0 ? null : OVSMap<T>.New(result.ToMap());
        }

        public IOVSField? DeserializeSet(JsonElement setElement, string typeName)
        {
            switch (setElement.ValueKind)
            {
                case JsonValueKind.Undefined: goto default;
                case JsonValueKind.Object: goto default;
                case JsonValueKind.Array:
                    break;
                case JsonValueKind.String:
                    // special case - convert value to string and create a set with a single value
                    var jsonStringValue = setElement.Deserialize<string>();
                    if (jsonStringValue == null) return null;

                    var targetValue = TypeDescriptor.GetConverter(typeof(T))
                        .ConvertFromInvariantString(jsonStringValue);
                    
                    if (targetValue == null)
                        return null;
                    
                    
                    
                    return OVSSet<T>.New(new[] { (T) targetValue }.ToSeq());

                case JsonValueKind.Number: goto default;
                case JsonValueKind.True: goto default;
                case JsonValueKind.False: goto default;
                case JsonValueKind.Null:
                    return OVSSet<T>.New(default);

                default:
                    throw new ArgumentOutOfRangeException(nameof(setElement));
            }


            var setRows = setElement.Deserialize<IEnumerable<JsonElement>>();

            if (setRows == null)
                return null;

            var result = new List<T>();
            foreach (var row in setRows)
            {
                T? value;
                if (row.ValueKind == JsonValueKind.Array)
                {
                    var rowData = row.Deserialize<JsonElement[]>();
                    if (rowData == null || rowData.Length == 0)
                        continue;


                    value = rowData.Length switch
                    {
                        1 => rowData[0].Deserialize<T>(),
                        //first field is type in this case
                        2 => rowData[1].Deserialize<T>(),
                        _ => default
                    };
                }
                else
                {
                    value = row.Deserialize<T>();
                }

                if (value != null) result.Add(value);
            }

            return result.Count == 0 ? null : OVSSet<T>.New(result.ToSeq());
        }
    }
}