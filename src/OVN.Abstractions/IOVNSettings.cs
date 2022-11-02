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
}