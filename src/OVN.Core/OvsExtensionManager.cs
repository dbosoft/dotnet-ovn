using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

/// <inheritdoc cref="IOvsExtensionManager"/>
public class OvsExtensionManager : IOvsExtensionManager
{
    public EitherAsync<Error, bool> IsExtensionEnabled() => true;
}
