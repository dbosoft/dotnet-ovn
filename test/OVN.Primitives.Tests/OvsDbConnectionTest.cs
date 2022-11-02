using System.Runtime.InteropServices;

namespace Dbosoft.OVN.Primitives.Tests;

public class OvsDbConnectionTest
{
    [Theory]
    [InlineData(false,"etc", "test.sock", "unix:/etc/test.sock", "LINUX")]
    [InlineData(false,"etc/test", "test.sock", "unix:/etc/test/test.sock", "LINUX")]
    [InlineData(false,"etc/test/", "test.sock", "unix:/etc/test/test.sock", "LINUX")]
    [InlineData(false,"etc", "test.sock", "unix:c:/openvswitch/etc/test.sock", "WINDOWS")]
    [InlineData(false,"etc/test", "test.sock", "unix:c:/openvswitch/etc/test/test.sock", "WINDOWS")]
    
    [InlineData(true,"etc", "test.sock", "punix:/etc/test.sock", "LINUX")]
    public void Pipe_command_string_has_expected_format(bool passive, string path, string name, string expectedString, string platform)
    {
        var ovsFile = new OvsFile(path, name);
        var fileSystem = new DefaultFileSystem(OSPlatform.Create(platform));

        var connection = new OvsDbConnection(ovsFile);
        Assert.Equal(expectedString, connection.GetCommandString(fileSystem, passive));
    }
    
    [Theory]
    [InlineData(false,"testhost", 4711, false, "tcp:testhost:4711")]
    [InlineData(false,"testhost", 4711, true, "ssl:testhost:4711")]
    [InlineData(true,"testhost", 4711, false, "ptcp:4711:testhost")]
    [InlineData(true,"testhost", 4711, true, "pssl:4711:testhost")]    
     public void Port_command_string_has_expected_format(bool passive, string address, int port, bool ssl, string expectedString)
    {
        var fileSystem = new DefaultFileSystem(OSPlatform.Create("linux"));

        var connection = new OvsDbConnection(address, port, ssl);
        Assert.Equal(expectedString, connection.GetCommandString(fileSystem, passive));
    }
}