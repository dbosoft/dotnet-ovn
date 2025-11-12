using Dbosoft.OVN.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _allowAttach,
                _useRemoteConfigsFromDatabase ? Some("db:OVN_Northbound,NB_Global,connections") : None);
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
                _allowAttach,
                _useRemoteConfigsFromDatabase ? Some("db:OVN_Southbound,SB_Global,connections") : None);
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
                _allowAttach,
                _useRemoteConfigsFromDatabase ? Some("db:Open_vSwitch,Open_vSwitch,connections") : None);
        }
    }
}
