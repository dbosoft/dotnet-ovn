namespace Dbosoft.OVN;

/// <summary>
/// Settings for full location installation of both OVS and OVN.
/// </summary>
public class LocalOVSWithOVNSettings : IOVNSettings
{
    /// <summary>
    /// Creates default settings.
    /// </summary>
    public LocalOVSWithOVNSettings()
    {
        NorthDBConnection = new OvsDbConnection(
            // ReSharper disable StringLiteralTypo
            new OvsFile("/var/run/ovn", "ovnnb_db.sock"));
        SouthDBConnection = new OvsDbConnection(
            new OvsFile("/var/run/ovn", "ovnsb_db.sock"));
        // ReSharper restore StringLiteralTypo
    }

    /// <inheritdoc />
    public OvsDbConnection NorthDBConnection { get; }

    /// <inheritdoc />
    public OvsDbConnection SouthDBConnection { get; }
}