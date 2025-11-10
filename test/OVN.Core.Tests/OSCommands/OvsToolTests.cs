using System.Diagnostics;
using Dbosoft.OVN.Model;
using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.TestTools;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using Moq;

namespace Dbosoft.OVN.Core.Tests.OSCommands;

public class OvsToolTests
{
    [Theory]
    [InlineData("peng", 0, "pong", "" )]
    [InlineData("peng", 0, "pong", "error_pong" )]
    [InlineData("ping", 1, "pong", "error_pong" )]
    public Task Runs_command_and_processes_output(string command, int returnCode, string output, string errorOutput)
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo, output, errorOutput, returnCode);
        
        var ovsTool = new DummyTool(envMock.Object);
        return ovsTool.RunAnyCommand(command)
            .Match(
                r =>
                {
                    if (returnCode != 0)
                        Assert.Equal("failure", "command succeed");
                    Assert.Equal(command, processStartInfo.Arguments);
                    // The error output is intentionally not included when a command exits successfully
                    Assert.Equal(output, r);
                },
                l =>
                {
                    if (returnCode == 0)
                        Assert.Equal("failure", "command succeed");

                    var expectedMessage = string.IsNullOrWhiteSpace(errorOutput) 
                        ? output 
                        : $"{output}\n{errorOutput}";

                    Assert.Contains(expectedMessage, l.Message);
                    
                    
                });
       
        
    }

    
    [Theory]
    [MemberData(nameof(JsonSamples))]
    public async Task GetRecord_json_response_is_correctly_parsed(string jsonData, LogicalRouterForTest expected)
    {
       var processStartInfo = new ProcessStartInfo();
       var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo, jsonData);
        
       var ovsTool = new DummyTool(envMock.Object);
       _ = await ovsTool.GetRecord<LogicalRouterForTest>("peng", "pong", cancellationToken: CancellationToken.None)
         // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
         .Match(r =>
          {
             Assert.Equal(expected, r);
             return Unit.Default;
          }, error => error.Throw());
       
    }
    
    [Theory]
    [MemberData(nameof(CommandSamples))]
    public async Task CreateRecord_generates_expected_arguments(LogicalRouterForTest sample, 
      OVSParentReference? reference,  string expectedCommand)
    {
      var processStartInfo = new ProcessStartInfo();
      var guidGeneratorMock = new Mock<IGuidGenerator>();
      guidGeneratorMock
          .Setup(m => m.GenerateGuid())
          .Returns(Guid.Parse("446035c9-1c80-4a8f-95b2-63adac02ac8f"));
      var envMock = OvsMocks.SetupEnvForOvsTool(
          processStartInfo,
          guidGenerator: guidGeneratorMock.Object);

      
      var ovsTool = new DummyTool(envMock.Object);
      _ = await ovsTool.CreateRecord("test_table",
          sample.ToMap(), 
          reference.ToOption(), 
          cancellationToken: CancellationToken.None)
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        .Match(r =>
        {
          Assert.Equal(expectedCommand, processStartInfo.Arguments);
          return Unit.Default;
        }, error => error.Throw());
       
    }
    
    [Fact]
    public async Task DestroyRecord_generates_expected_arguments()
    {
      var processStartInfo = new ProcessStartInfo();
      var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
      const string expectedCommand = "destroy test_table test_row";
      
      var ovsTool = new DummyTool(envMock.Object);
      _ = await ovsTool.RemoveRecord("test_table", "test_row",
          cancellationToken: CancellationToken.None)
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        .Match(r =>
        {
          Assert.Equal(expectedCommand, processStartInfo.Arguments);
          return Unit.Default;
        }, error => error.Throw());
       
    }
    
    [Fact]
    public async Task RemoveColumnValue_generates_expected_arguments()
    {
      var processStartInfo = new ProcessStartInfo();
      var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
      const string expectedCommand = "remove test_table test_row test_column value";
      
      var ovsTool = new DummyTool(envMock.Object);
      _ = await ovsTool.RemoveColumnValue(
        "test_table", 
        "test_row", 
        "test_column",
        "value",
          cancellationToken: CancellationToken.None)
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        .Match(r =>
        {
          Assert.Equal(expectedCommand, processStartInfo.Arguments);
          return Unit.Default;
        }, error => error.Throw());
       
    }
    
    [Fact]
    public async Task FindRecords_generates_expected_arguments()
    {
      var processStartInfo = new ProcessStartInfo();
      var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
      const string expectedCommand = "remove test_table test_row test_column value";
      
      var ovsTool = new DummyTool(envMock.Object);
      _ = await ovsTool.RemoveColumnValue(
          "test_table", 
          "test_row", 
          "test_column",
          "value",
          cancellationToken: CancellationToken.None)
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        .Match(r =>
        {
          Assert.Equal(expectedCommand, processStartInfo.Arguments);
          return Unit.Default;
        }, error => error.Throw());
       
    }


    public static IEnumerable<object[]> JsonSamples()
    {
       yield return new object[] { JsonSampleDataSetWithSingleValue, ResultFromDataSetWithSingleValue };
       
       yield return new object[] { JsonSampleDataSetWithTwoValues, ResultFromDataSetWithTwoValues };
    
    }
    
    public static IEnumerable<object[]> CommandSamples()
    {
      yield return new object[] { ResultFromDataSetWithSingleValue, 
        null!,
        "-- --id=446035c9-1c80-4a8f-95b2-63adac02ac8f create test_table _uuid=\"5a767e54-5a71-4e76-b2ff-2695f7d2771c\" "+ 
        "external_ids={test=\"\\\"t1\\\"\",test2=\"\\\"t2\\\"\"} name=\"\\\"hello_router\\\"\""+
        " ports=[\"\\\"06490156-e71b-4dee-8755-6225f93c68a3\\\"\"]" };

      yield return new object[]
      {
        ResultFromDataSetWithSingleValue,
        new OVSParentReference("ref_table", "parent", "ref_column"),
        "-- --id=446035c9-1c80-4a8f-95b2-63adac02ac8f create test_table _uuid=\"5a767e54-5a71-4e76-b2ff-2695f7d2771c\" "+
        "external_ids={test=\"\\\"t1\\\"\",test2=\"\\\"t2\\\"\"} name=\"\\\"hello_router\\\"\" "+
        "ports=[\"\\\"06490156-e71b-4dee-8755-6225f93c68a3\\\"\"] "+
        "-- add ref_table parent ref_column 446035c9-1c80-4a8f-95b2-63adac02ac8f"
      };

    }
    
    
    private static readonly LogicalRouterForTest ResultFromDataSetWithSingleValue = new()
    {
        Id = Guid.Parse("5a767e54-5a71-4e76-b2ff-2695f7d2771c"),
        Name = "hello_router",
        ExternalIds = new Dictionary<string,string> { { "test", "t1" }, { "test2", "t2" } }.ToMap(),
        Ports = new []{ "06490156-e71b-4dee-8755-6225f93c68a3" }.ToSeq()
    };

    private static readonly LogicalRouterForTest ResultFromDataSetWithTwoValues = new()
    {
        Id = Guid.Parse("5a767e54-5a71-4e76-b2ff-2695f7d2771c"),
        Name = "hello_router",
        ExternalIds = new Dictionary<string, string> { { "test", "t1" }, { "test2", "t2" } }.ToMap(),
        Ports = new []{ "1c3484e2-8984-45ec-882a-be6d3ca6dad4", "d7cd446d-84c6-4694-8f9d-16ddbaaceb99" }.ToSeq()
    };

    private const string JsonSampleDataSetWithSingleValue = @"
{
  ""data"": [
    [
      [
        ""uuid"",
        ""5a767e54-5a71-4e76-b2ff-2695f7d2771c""],
      [
        ""set"",
        []],
      [
        ""set"",
        []],
      [
        ""map"",
        [
          [
            ""test"",
            ""t1""],
          [
            ""test2"",
            ""t2""]]],
      [
         ""set"",
        []],
      [
        ""set"",
        []],
      ""hello_router"",
      [
        ""set"",
        []],
      [
        ""map"",
        []],
      [
        ""set"",
        []],
      [
        ""uuid"",
        ""06490156-e71b-4dee-8755-6225f93c68a3""],
      [
        ""set"",
        []]]],
  ""headings"": [
    ""_uuid"",
    ""copp"",
    ""enabled"",
    ""external_ids"",
    ""load_balancer"",
    ""load_balancer_group"",
    ""name"",
    ""nat"",
    ""options"",
    ""policies"",
    ""ports"",
    ""static_routes""]
}
";


    private const string JsonSampleDataSetWithTwoValues = @"
{
  ""data"": [
    [
      [
        ""uuid"",
        ""5a767e54-5a71-4e76-b2ff-2695f7d2771c""],
      [
        ""set"",
        []],
      [
        ""set"",
        []],
      [
        ""map"",
        [
          [
            ""test"",
            ""t1""],
          [
            ""test2"",
            ""t2""]]],
      [
         ""set"",
        []],
      [
        ""set"",
        []],
      ""hello_router"",
      [
        ""set"",
        []],
      [
        ""map"",
        []],
      [
        ""set"",
        []],
       [
        ""set"",
        [
          [
            ""uuid"",
            ""1c3484e2-8984-45ec-882a-be6d3ca6dad4""],
          [
            ""uuid"",
            ""d7cd446d-84c6-4694-8f9d-16ddbaaceb99""]]],
      [
        ""set"",
        []]]],
  ""headings"": [
    ""_uuid"",
    ""copp"",
    ""enabled"",
    ""external_ids"",
    ""load_balancer"",
    ""load_balancer_group"",
    ""name"",
    ""nat"",
    ""options"",
    ""policies"",
    ""ports"",
    ""static_routes""]
}
";


    public record LogicalRouterForTest : OVSTableRecord
    {
      [UsedImplicitly] public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
          {"name", OVSValue<string>.Metadata()},
          {"ports", OVSSet<string>.Metadata()}
        };
      
      public string? Name
      {
        get => GetValue<string>("name");
        init => SetValue("name", value);
      } 
      
      public new Guid Id
      {
        get => GetValue<Guid>("_uuid");
        set => SetValue("_uuid", value);
      }
      
      public Seq<string> Ports
      {
        get => GetSet<string>("ports");
        set => SetSet("ports", value);
      }
    }
    
  private class DummyTool : OVSTool
    {
       public DummyTool(ISystemEnvironment systemEnvironment) : base(systemEnvironment, new OvsFile("", "puff", true))
       {
       }

       public EitherAsync<Error, string> RunAnyCommand(string command)
       {
          return RunCommandWithResponse(command);
       }
    }
}