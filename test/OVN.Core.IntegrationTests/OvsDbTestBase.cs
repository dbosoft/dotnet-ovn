using Dbosoft.OVN.OSCommands.OVS;
using LanguageExt;
using LanguageExt.Common;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Core.IntegrationTests;

public abstract class OvsDbTestBase : IAsyncLifetime
{
    private readonly string _dataDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        .Replace(@"\", "/");
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
        _dbSettings = dbSettings;
    }

    public virtual async Task InitializeAsync()
    {
        if (Directory.Exists(_dataDirectoryPath))
            // Should not happen as we use a random directory name
            Assert.Fail($"The data directory '{_dataDirectoryPath}' already exists.");

        var serverExecutable = SystemEnvironment.FileSystem.ResolveOvsFilePath(OVSCommands.DBServer);
        if (!File.Exists(serverExecutable))
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

    protected async Task VerifyDatabase(string databaseName, string schemaVersion)
    {
        var ovsDbClientTool = new OVSDbClientTool(SystemEnvironment, _dbSettings.Connection);
        var dbSchemaVersion = (await ovsDbClientTool.GetSchemaVersion(databaseName)).ThrowIfLeft();
        if (dbSchemaVersion != schemaVersion)
            Assert.Fail($"""
                        Schema version mismatch detected!
                        The tests were created for schema version '{schemaVersion}', but the database
                        reports schema version '{dbSchemaVersion}'. Is the correct version of OVS
                        and OVN used to run the tests?
                        """);
        var dump = (await ovsDbClientTool.PrintDatabase(databaseName)).ThrowIfLeft();
        var settings = new VerifySettings();
        settings.AddScrubber(FixedLengthGuidScrubber.ReplaceGuids);
        settings.AddScrubber(FixedLengthHashScrubber.ReplaceHashes);
        settings.AddScrubber(builder =>
        {
            builder.Replace(
                _dataDirectoryPath,
                $"{{OvsTestDirectory{new string('_', _dataDirectoryPath.Length - "OvsTestDirectory".Length - 2)}}}");
        });
        await Verify(dump, settings);
    }

    public async Task DisposeAsync()
    {
        (await _ovsDbProcess.Stop(true, CancellationToken.None)).ThrowIfLeft();
    }
}
