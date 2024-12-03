using LanguageExt.Common;

namespace Dbosoft.OVNAgent;

static class ErrorUtils
{
    public static string PrintError(Error error) => error switch
    {
        ManyErrors me => string.Join(Environment.NewLine, me.Errors.Map(PrintError)),
        Exceptional ee => ee.ToException().ToString(),
        _ => error.Message
             + error.Inner.Map(i => $"{Environment.NewLine}{PrintError(i)}").IfNone(""),
    };
}
