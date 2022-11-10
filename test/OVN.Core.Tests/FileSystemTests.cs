using System.Runtime.InteropServices;

namespace Dbosoft.OVN.Core.Tests;

public class FileSystemTests
{
    [Theory]
    [InlineData("var", "test.sock", false, true, "LINUX", "/var/test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, true, "LINUX", "/var/ovstest/test.sock" )]
    [InlineData("usr/bin", "test", true, true, "LINUX", "/usr/bin/test" )]
    [InlineData("usr/sbin", "test", true, true, "LINUX", "/usr/sbin/test" )]
    
    [InlineData("var", "test.sock", false, true, "WINDOWS", "C:/ProgramData/openvswitch/var/test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, true, "WINDOWS", "C:/ProgramData/openvswitch/var/ovstest/test.sock" )]
    [InlineData("usr/bin", "test", true, true, "WINDOWS", "c:/openvswitch/usr/bin/test.exe" )]
    [InlineData("usr/sbin", "test", true, true, "WINDOWS", "c:/openvswitch/usr/sbin/test.exe" )]
    
    [InlineData("var", "test.sock", false, false, "WINDOWS", "C:\\ProgramData\\openvswitch\\var\\test.sock" )]
    [InlineData("var/ovstest", "test.sock", false, false, "WINDOWS", "C:\\ProgramData\\openvswitch\\var\\ovstest\\test.sock" )]
    [InlineData("usr/bin", "test", true, false, "WINDOWS", "c:\\openvswitch\\usr\\bin\\test.exe" )]
    [InlineData("usr/sbin", "test", true, false, "WINDOWS", "c:\\openvswitch\\usr\\sbin\\test.exe" )]

    
    public void OvsFile_is_resolved_to_expected_path(string path, string name, bool isExe, bool platformNeutral, string platform, string expectedPath)
    {
        var ovsFile = new OvsFile(path, name, isExe);
        var fileSystem = new DefaultFileSystem(OSPlatform.Create(platform));
        
        Assert.Equal(expectedPath, fileSystem.ResolveOvsFilePath(ovsFile, platformNeutral));
    }
}