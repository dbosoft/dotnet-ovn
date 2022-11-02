using System.Runtime.InteropServices;

namespace Dbosoft.OVN.Core.Tests;

public class FileSystemTests
{
    [Theory]
    [InlineData("var", "test.sock", false, true, "LINUX", "/var/test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, true, "LINUX", "/var/ovstest/test.sock" )]
    [InlineData("var/bin", "test", true, true, "LINUX", "/var/bin/test" )]
    [InlineData("var/sbin", "test", true, true, "LINUX", "/var/sbin/test" )]
    
    [InlineData("var", "test.sock", false, true, "WINDOWS", "c:/openvswitch/var/test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, true, "WINDOWS", "c:/openvswitch/var/ovstest/test.sock" )]
    [InlineData("var/bin", "test", true, true, "WINDOWS", "c:/openvswitch/var/bin/test.exe" )]
    [InlineData("var/sbin", "test", true, true, "WINDOWS", "c:/openvswitch/var/sbin/test.exe" )]
    
    [InlineData("var", "test.sock", false, false, "WINDOWS", "c:\\openvswitch\\var\\test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, false, "WINDOWS", "c:\\openvswitch\\var\\ovstest\\test.sock" )]
    [InlineData("var/bin", "test", true, false, "WINDOWS", "c:\\openvswitch\\var\\bin\\test.exe" )]
    [InlineData("var/sbin", "test", true, false, "WINDOWS", "c:\\openvswitch\\var\\sbin\\test.exe" )]

    
    public void OvsFile_is_resolved_to_expected_path(string path, string name, bool isExe, bool platformNeutral, string platform, string expectedPath)
    {
        var ovsFile = new OvsFile(path, name, isExe);
        var fileSystem = new DefaultFileSystem(OSPlatform.Create(platform));
        
        Assert.Equal(expectedPath, fileSystem.ResolveOvsFilePath(ovsFile, platformNeutral));
    }
}