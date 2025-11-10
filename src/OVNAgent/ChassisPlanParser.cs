using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN;

namespace Dbosoft.OVNAgent;

public static class ChassisPlanParser
{
    public static ChassisPlan ParseYaml(IDictionary<object, object> yamlData)
    {

        var plan = new ChassisPlan("");
        return plan;
    }
}
