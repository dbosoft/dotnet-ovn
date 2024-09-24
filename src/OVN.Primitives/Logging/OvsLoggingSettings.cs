using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.Logging;

public class OvsLoggingSettings
{
    public OvsLoggingFileSettings File { get; set; } = new();
}