using System.Diagnostics;
using Dbosoft.OVN.OSCommands.OVS;
using Dbosoft.OVN.TestTools;

namespace Dbosoft.OVN.Core.Tests.OSCommands.OVS;

public class OvsDbToolTests
{
    [Theory]
    [InlineData("/etc", "db.sock", "schema.data", "create \"/etc/db.sock\" \"/etc/schema.data\"")]
    public Task Creates_DbFile_with_Scheme(string path, string dbFile, string schemaFile, string command)
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        var ovsDb = new OvsFile(path, dbFile);
        var schema = new OvsFile(path, schemaFile);

        var ovsDBTool = new OVSDBTool(envMock.Object);
        return ovsDBTool.CreateDBFile(ovsDb, schema)
            .Match(
                _ =>
                {
                    Assert.Equal(command, processStartInfo.Arguments);
                },
                l => Assert.Equal("", l.Message));
       
        
    }
}