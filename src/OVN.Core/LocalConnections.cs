namespace Dbosoft.OVN;

public static class LocalConnections
{
    public static readonly OvsDbConnection Northbound = new(new OvsFile("/var/run/ovn", "ovnnb_db.sock"));

    public static readonly OvsDbConnection Southbound = new(new OvsFile("/var/run/ovn", "ovnsb_db.sock"));

    public static readonly OvsDbConnection Switch = new(new OvsFile("/var/run/openvswitch", "db.sock"));
}
