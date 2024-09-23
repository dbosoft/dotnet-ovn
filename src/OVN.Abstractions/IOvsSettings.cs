using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN;

/// <summary>
/// Abstraction of OVS Settings.
/// </summary>
public interface IOvsSettings
{
    /// <summary>
    /// The settings for the logging.
    /// </summary>
    public OvsLoggingSettings Logging { get; }
}
