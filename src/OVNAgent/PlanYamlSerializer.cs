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

    public static TConfig Deserialize<TConfig>(string yaml) where TConfig : class
    {
        return Deserializer.Value.Deserialize<TConfig>(yaml);
    }
}
