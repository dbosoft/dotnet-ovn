using Dbosoft.OVN.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Model.OVN;
using Dbosoft.OVN.Model.OVS;
using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands.OVS;

public abstract class OVSDbSettingsBuilder
{
    protected OvsDbConnection? _dbConnection;
    protected OvsLoggingSettings _loggingSettings = new();
    protected bool _allowAttach;
    protected bool _useRemoteConfigsFromDatabase;

    public static OVSDbSettingsBuilder ForNorthbound() => new NorthboundDbSettingsBuilder();

    public static OVSDbSettingsBuilder ForSouthbound() => new SouthboundDbSettingsBuilder();

    public static OVSDbSettingsBuilder ForSwitch() => new SwitchDbSettingsBuilder();

    public OVSDbSettingsBuilder WithDbConnection(OvsDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
        return this;
    }

    public OVSDbSettingsBuilder UseRemoteConfigsFromDatabase(
        bool useConnectionsFromDb)
    {
        _useRemoteConfigsFromDatabase = useConnectionsFromDb;
        return this;
    }

    public OVSDbSettingsBuilder AllowAttach(bool allow)
    {
        _allowAttach = allow;
        return this;
    }

    public OVSDbSettingsBuilder WithLogging(OvsLoggingSettings loggingSettings)
    {
        _loggingSettings = loggingSettings;
        return this;
    }

    public abstract OVSDbSettings Build();

    private class NorthboundDbSettingsBuilder : OVSDbSettingsBuilder
    {
        public override OVSDbSettings Build()
        {
            return new OVSDbSettings(
                _dbConnection ?? LocalConnections.Northbound,
                new OvsFile("etc/ovn", "ovn_nb.db"),
                new OvsFile("usr/share/ovn", "ovn-nb.ovsschema"),
                new OvsFile("var/run/ovn", "ovn_nb.ctl"),
                new OvsFile("var/log/ovn", "ovn-nb.log"),
                _loggingSettings,
                "OVN_Northbound",
                OVNTableNames.Global,
                _allowAttach,
                _useRemoteConfigsFromDatabase);
        }
    }

    private class SouthboundDbSettingsBuilder : OVSDbSettingsBuilder
    {
        public override OVSDbSettings Build()
        {
            return new OVSDbSettings(
                _dbConnection ?? LocalConnections.Southbound,
                new OvsFile("etc/ovn", "ovn_sb.db"),
                new OvsFile("usr/share/ovn", "ovn-sb.ovsschema"),
                new OvsFile("var/run/ovn", "ovn_sb.ctl"),
                new OvsFile("var/log/ovn", "ovn-sb.log"),
                _loggingSettings,
                "OVN_Southbound",
                OVNSouthboundTableNames.Global,
                _allowAttach,
                _useRemoteConfigsFromDatabase);
        }
    }

    private class SwitchDbSettingsBuilder : OVSDbSettingsBuilder
    {
        public override OVSDbSettings Build()
        {
            return new OVSDbSettings(
                _dbConnection ?? LocalConnections.Switch,
                new OvsFile("etc/openvswitch", "ovs.db"),
                new OvsFile("usr/share/openvswitch", "vswitch.ovsschema"),
                new OvsFile("var/run/openvswitch", "ovs-db.ctl"),
                new OvsFile("var/log/openvswitch", "ovs-db.log"),
                _loggingSettings,
                "Open_vSwitch",
                OVSTableNames.Global,
                _allowAttach,
                _useRemoteConfigsFromDatabase);
        }
    }
}
