using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Nodes;
using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Core.IntegrationTests;

public abstract class OvsDbTestBase : IAsyncLifetime
{
    private readonly string _dataDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly ILoggerFactory _loggerFactory;
    protected readonly ISystemEnvironment SystemEnvironment;

    private OVSDBProcess _ovsDbProcess = null!;
    protected readonly OvsDbConnection DbConnection = LocalConnections.Northbound;

    protected OvsDbTestBase(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder
            .AddProvider(new XUnitLoggerProvider(testOutputHelper, new XUnitLoggerOptions()))
            .SetMinimumLevel(LogLevel.Trace));

        SystemEnvironment = new TestSystemEnvironment(_loggerFactory, _dataDirectoryPath);
    }

    public virtual async Task InitializeAsync()
    {
        // TODO Improve
        if (Directory.Exists(_dataDirectoryPath))
            throw new InvalidOperationException("The directory already exists!");

        Directory.CreateDirectory(_dataDirectoryPath);

        _ovsDbProcess = new OVSDBProcess(
            SystemEnvironment,
            OVSDBSettingsBuilder.ForNorthbound()
                .WithDbConnection(DbConnection)
                .Build(),
            _loggerFactory.CreateLogger<OVSDBProcess>());

        (await InitializeDatabase()).ThrowIfLeft();
    }

    private EitherAsync<Error, Unit> InitializeDatabase() =>
        from _1 in _ovsDbProcess.Start()
        from _2 in DbConnection.WaitForDbSocket(SystemEnvironment, CancellationToken.None).ToAsync()
        select unit;

    protected async Task<string> DumpDatabase(string databaseName)
    {
        var ovsDbClientTool = new OVSDbClientTool(SystemEnvironment, DbConnection);
        var result = (await ovsDbClientTool.DumpDatabase(databaseName)).ThrowIfLeft();
        var json = $"[\n{result.Replace("}\n", "},\n").Replace("}\r\n", "},\r\n")}\n]";
        return json;
    }

    protected async Task VerifyDatabase(string databaseName)
    {
        var json = await DumpDatabase(databaseName);
        await VerifyJson(json);
    }

    public async Task DisposeAsync()
    {
        (await _ovsDbProcess.Stop(true, CancellationToken.None)).ThrowIfLeft();
    }

    private static string PrepareTempDirectory()
    {
        string path;
        do
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        } while (Directory.Exists(path));

        Directory.CreateDirectory(path);

        return path;
    }
}
