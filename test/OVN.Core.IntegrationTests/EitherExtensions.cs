using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class EitherExtensions
{
    public static TRight ThrowIfLeft<TRight>(this Either<Error, TRight> either)
    {
        return either.Match(
            Right: r => r,
            Left: e => e.ToException().Rethrow<TRight>());
    }
}
