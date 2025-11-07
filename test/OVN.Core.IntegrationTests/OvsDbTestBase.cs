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
    private readonly OVSDbSettings _dbSettings;

    protected OvsDbTestBase(
        ITestOutputHelper testOutputHelper,
        OVSDbSettings dbSettings)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder
            .AddProvider(new XUnitLoggerProvider(testOutputHelper, new XUnitLoggerOptions()))
            .SetMinimumLevel(LogLevel.Debug));

        
        SystemEnvironment = new TestSystemEnvironment(_loggerFactory, _dataDirectoryPath);
        this._dbSettings = dbSettings;
    }

    public virtual async Task InitializeAsync()
    {
        if (Directory.Exists(_dataDirectoryPath))
            // Should not happen as we use a random directory name
            Assert.Fail($"The data directory '{_dataDirectoryPath}' already exists.");

        var serverExecutable = SystemEnvironment.FileSystem.ResolveOvsFilePath(OVSCommands.DBServer);
        if(!File.Exists(serverExecutable))
            Assert.Fail("OVN is not installed.");

        Directory.CreateDirectory(_dataDirectoryPath);

        _ovsDbProcess = new OVSDBProcess(
            SystemEnvironment,
            _dbSettings,
            _loggerFactory.CreateLogger<OVSDBProcess>());

        (await StartDatabase()).ThrowIfLeft();
    }

    private EitherAsync<Error, Unit> StartDatabase() =>
        from _1 in _ovsDbProcess.Start()
        from _2 in _dbSettings.Connection.WaitForDbSocket(SystemEnvironment, CancellationToken.None).ToAsync()
        from _3 in InitializeDatabase()
        select unit;

    protected abstract EitherAsync<Error, Unit> InitializeDatabase();

    protected async Task VerifyDatabase(string databaseName)
    {
        var ovsDbClientTool = new OVSDbClientTool(SystemEnvironment, _dbSettings.Connection);
        var dump = (await ovsDbClientTool.PrintDatabase(databaseName)).ThrowIfLeft();
        var settings = new VerifySettings();
        settings.AddScrubber(FixedLengthGuidScrubber.ReplaceGuids);
        settings.AddScrubber(sb => { sb.Replace(_dataDirectoryPath.Replace(@"\", @"\\"), "{OvsDataDirectory}"); });
        await Verify(dump, settings);
    }

    public async Task DisposeAsync()
    {
        (await _ovsDbProcess.Stop(true, CancellationToken.None)).ThrowIfLeft();
    }
}
