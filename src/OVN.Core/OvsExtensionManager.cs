using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

/// <inheritdoc cref="IOvsExtensionManager"/>
internal class OvsExtensionManager : IOvsExtensionManager
{
    public EitherAsync<Error, bool> IsExtensionEnabled() => true;
}
