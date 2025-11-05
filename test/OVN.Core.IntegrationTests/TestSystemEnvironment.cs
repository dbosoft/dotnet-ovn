using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class TestSystemEnvironment(
    ILoggerFactory loggerFactory,
    string dataDirectoryPath)
    : SystemEnvironment(loggerFactory)
{
    public override IFileSystem FileSystem => new TestFileSystem(GetPlatform(), dataDirectoryPath);
}
