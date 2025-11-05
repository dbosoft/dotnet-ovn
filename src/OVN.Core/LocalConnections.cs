using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN;

public static class LocalConnections
{
    public static OvsDbConnection Northbound { get; } = new(new OvsFile("/var/run/ovn", "ovnnb_db.sock"));

    public static OvsDbConnection Southbound { get; } = new(new OvsFile("/var/run/ovn", "ovnsb_db.sock"));

    public static OvsDbConnection Switch { get; } = new(new OvsFile("/var/run/openvswitch", "db.sock"));
}
