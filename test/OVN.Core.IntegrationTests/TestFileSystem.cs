using System.Runtime.InteropServices;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class TestFileSystem(OSPlatform platform, string dataPath)
    : DefaultFileSystem(platform)
{
    protected override string GetDataRootPath() => $"{dataPath}/";

    protected override void SetAdminOnlyPermissions(DirectoryInfo directoryInfo)
    {
        // We intentionally do not set directory permissions in the
        // integration tests as the tests are expected to run in
        // the context of a normal user.
    }
}
