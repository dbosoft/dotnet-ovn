using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model;

/// <summary>
/// Common used queries
/// </summary>
[PublicAPI]
public static class CommonQueries
{
    /// <summary>
    /// Query by name of records
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Map<string, OVSQuery> Name(string name)
    {
        return new Dictionary<string, OVSQuery> { { "name", new OVSQuery("=", OVSValue<string>.New(name)) } }.ToMap();
    }

    /// <summary>
    /// Query by external ids
    /// </summary>
    /// <param name="ids">map of external ids</param>
    /// <returns></returns>
    public static Map<string, OVSQuery> ExternalIds(Map<string, string> ids)
    {
        return new Dictionary<string, OVSQuery>
            { { "external_ids", new OVSQuery("=", OVSMap<string>.New(ids)) } }.ToMap();
    }

    /// <summary>
    /// Query by a single external id.
    /// </summary>
    /// <param name="id">external id</param>
    /// <param name="value">value of external id.</param>
    /// <returns></returns>
    public static Map<string, OVSQuery> ExternalId(string id, string value)
    {
        return ExternalIds(new Dictionary<string, string> { { id, value } }.ToMap());
    }
}