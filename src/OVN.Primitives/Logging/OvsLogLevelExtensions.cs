using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Logging;

public static class OvsLogLevelExtensions
{
    public static Option<OvsLogLevel> ParseOvsValue(string ovsValue) =>
        ovsValue.ToLowerInvariant() switch
        {
            "off" => OvsLogLevel.Off,
            "emer" => OvsLogLevel.Emergency,
            "err" => OvsLogLevel.Error,
            "warn" => OvsLogLevel.Warning,
            "info" => OvsLogLevel.Info,
            "dbg" => OvsLogLevel.Debug,
            _ => None,
        };

    public static string ToOvsValue(this OvsLogLevel logLevel) =>
        logLevel switch
        {
            OvsLogLevel.Off => "off",
            OvsLogLevel.Emergency => "emer",
            OvsLogLevel.Error => "err",
            OvsLogLevel.Warning => "warn",
            OvsLogLevel.Info => "info",
            OvsLogLevel.Debug => "dbg",
            _ => throw new ArgumentOutOfRangeException(
                nameof(logLevel),
                logLevel,
                "The log level is not supported"),
        };
}
