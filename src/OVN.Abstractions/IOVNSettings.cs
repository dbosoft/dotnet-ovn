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

    /// <summary>
    /// The log level when logging to files. off disables the file logging.
    /// </summary>
    public string LogFileLevel { get; set; }
}