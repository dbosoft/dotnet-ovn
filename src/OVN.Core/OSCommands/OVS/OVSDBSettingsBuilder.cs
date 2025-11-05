using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

public abstract class OVSDBSettingsBuilder
{
    protected OvsDbConnection? _dbConnection;
    protected OvsLoggingSettings _loggingSettings = new OvsLoggingSettings();
    protected bool _allowAttach;

    public static OVSDBSettingsBuilder ForNorthbound() => new NorthboundDbSettingsBuilder();

    public static OVSDBSettingsBuilder ForSouthbound() => new SouthboundDbSettingsBuilder();

    public static OVSDBSettingsBuilder ForSwitch() => new SwitchDbSettingsBuilder();

    public OVSDBSettingsBuilder WithDbConnection(OvsDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
        return this;
    }

    public OVSDBSettingsBuilder AllowAttach(bool allow)
    {
        _allowAttach = allow;
        return this;
    }

    public OVSDBSettingsBuilder WithLogging(OvsLoggingSettings loggingSettings)
    {
        _loggingSettings = loggingSettings;
        return this;
    }

    public abstract OVSDbSettings Build();

    private class NorthboundDbSettingsBuilder : OVSDBSettingsBuilder
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
                false);
        }
    }

    private class SouthboundDbSettingsBuilder : OVSDBSettingsBuilder
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
                _allowAttach);
        }
    }

    private class SwitchDbSettingsBuilder : OVSDBSettingsBuilder
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
                _allowAttach);
        }
    }
}
