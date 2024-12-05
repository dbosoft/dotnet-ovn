using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

public interface IOvsExtensionManager
{
    EitherAsync<Error, bool> IsExtensionEnabled();
}
