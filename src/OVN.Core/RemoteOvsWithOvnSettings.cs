using System.Net;
using Dbosoft.OVN.Logging;
using LanguageExt;

namespace Dbosoft.OVN;

public class RemoteOvsWithOvnSettings : IOVNSettings, IOvsSettings
{
    /// <summary>
    /// Creates default settings.
    /// </summary>
    public RemoteOvsWithOvnSettings(
        OvsDbConnection southDbConnection,
        string chassisName,
        IPAddress? encapIp,
        Map<string, string> bridgeMappings = default)
    {
        NorthDBConnection = new OvsDbConnection(
            // ReSharper disable StringLiteralTypo
            new OvsFile("/var/run/ovn", "ovnnb_db.sock"));
        SouthDBConnection = southDbConnection;
        ChassisName = chassisName;
        EncapId = encapIp;
        BridgeMappings = bridgeMappings;
        // ReSharper restore StringLiteralTypo
    }

    /// <inheritdoc />
    public OvsDbConnection NorthDBConnection { get; }

    /// <inheritdoc />
    public OvsDbConnection SouthDBConnection { get; }

    /// <inheritdoc cref="IOvsSettings.Logging" />
    public OvsLoggingSettings Logging { get; set; } = new();

    public string ChassisName { get; }

    public IPAddress? EncapId { get; }

    public Map<string, string> BridgeMappings { get; }
}
