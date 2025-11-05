using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class TestFileSystem : DefaultFileSystem
{
    private readonly string _dataPath;

    public TestFileSystem(OSPlatform platform, string dataPath) : base(platform)
    {
        _dataPath = dataPath;
    }

    protected override string GetDataRootPath() => $"{_dataPath}/";
}
