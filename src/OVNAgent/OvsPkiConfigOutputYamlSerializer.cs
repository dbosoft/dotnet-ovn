using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dbosoft.OVNAgent;

public static class OvsPkiConfigOutputYamlSerializer
{
    private static readonly Lazy<ISerializer> Serializer = new(() =>
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull
                                            | DefaultValuesHandling.OmitEmptyCollections)
            .WithAttributeOverride<OvsPkiConfigOutput>(
                c => c.PrivateKey,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .WithAttributeOverride<OvsPkiConfigOutput>(
                c => c.Certificate,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .WithAttributeOverride<OvsPkiConfigOutput>(
                c => c.CaCertificate,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .DisableAliases()
            .Build());

    public static string Serialize(OvsPkiConfigOutput config)
    {
        return Serializer.Value.Serialize(config);
    }
}
