// Based on https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/extensibility.md
using AwesomeAssertions.Execution;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class DirectoryInfoExtensions
{
    public static DirectoryInfoAssertions Should(this DirectoryInfo instance)
    {
        return new DirectoryInfoAssertions(instance, AssertionChain.GetOrCreate());
    }
}
