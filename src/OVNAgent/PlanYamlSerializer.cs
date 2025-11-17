using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dbosoft.OVNAgent;

public static class PlanYamlSerializer
{
    private static readonly Lazy<IDeserializer> Deserializer = new(() =>
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithEnforceRequiredMembers()
            .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
            .Build());

    private static readonly Lazy<ISerializer> Serializer = new(() =>
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull
                                            | DefaultValuesHandling.OmitEmptyCollections)
            .WithAttributeOverride<ChassisPkiOutput>(
                c => c.PrivateKey,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .WithAttributeOverride<ChassisPkiOutput>(
                c => c.Certificate,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .WithAttributeOverride<ChassisPkiOutput>(
                c => c.CaCertificate,
                new YamlMemberAttribute { ScalarStyle = ScalarStyle.Literal })
            .DisableAliases()
            .Build());

    public static TConfig Deserialize<TConfig>(string yaml) where TConfig : class
    {
        return Deserializer.Value.Deserialize<TConfig>(yaml);
    }

    public static string Serialize<TConfig>(TConfig config) where TConfig : class
    {
        return Serializer.Value.Serialize(config);
    }
}
