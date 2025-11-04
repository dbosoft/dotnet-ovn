using System.Net;
using Dbosoft.OVN.Logging;
using LanguageExt;

namespace Dbosoft.OVN;

/// <summary>
/// Abstraction of OVN Settings. 
/// </summary>
public interface IOVNSettings
{
    /// <summary>
    /// Connection to Northbound DB.
    /// </summary>
    public OvsDbConnection NorthDBConnection { get; }
    
    /// <summary>
    /// Connection to Southbound DB.
    /// </summary>
    public OvsDbConnection SouthDBConnection { get; }

    public string ChassisName { get; }

    public IPAddress? EncapId { get; }

    Map<string, string> BridgeMappings { get; }

    /// <summary>
    /// The settings for the logging.
    /// </summary>
    public OvsLoggingSettings Logging { get; set; }
}
