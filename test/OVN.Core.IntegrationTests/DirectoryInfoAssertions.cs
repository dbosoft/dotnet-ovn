// Based on https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/extensibility.md
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class DirectoryInfoAssertions(
    DirectoryInfo instance,
    AssertionChain chain)
    : ReferenceTypeAssertions<DirectoryInfo, DirectoryInfoAssertions>(instance, chain)
{
    protected override string Identifier => "directory";

    [CustomAssertion]
    public AndWhichConstraint<DirectoryInfoAssertions, DirectoryInfo> ContainDirectory(
        string directoryName, string because = "", params object[] becauseArgs)
    {
        chain.OverrideCallerIdentifier(() => Subject.FullName);

        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(directoryName))
            .FailWith("You can't assert a directory exist if you don't pass a proper name")
            .Then
            .Given(() => Subject.GetDirectories())
            .ForCondition(directories => directories.Any(directoryInfo => directoryInfo.Name.Equals(directoryName)))
            .FailWith("Expected {context:directory} to contain {0}{reason}, but found {1}.",
                _ => directoryName,
                directories => directories.Select(directory => directory.Name));

        return new AndWhichConstraint<DirectoryInfoAssertions, DirectoryInfo>(
            this,
            Subject.GetDirectories().First(directoryInfo => directoryInfo.Name.Equals(directoryName)));
    }

    [CustomAssertion]
    public AndConstraint<DirectoryInfoAssertions> ContainFile(
        string filename, string because = "", params object[] becauseArgs)
    {
        chain.OverrideCallerIdentifier(() => Subject.FullName);

        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(filename))
            .FailWith("You can't assert a file exist if you don't pass a proper name")
            .Then
            .Given(() => Subject.GetFiles())
            .ForCondition(files => files.Any(fileInfo => fileInfo.Name.Equals(filename)))
            .FailWith("Expected {context:directory} to contain {0}{reason}, but found {1}.",
                _ => filename, files => files.Select(file => file.Name));

        return new AndConstraint<DirectoryInfoAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<DirectoryInfoAssertions> NotContainFile(
        string filename, string because = "", params object[] becauseArgs)
    {
        chain.OverrideCallerIdentifier(() => Subject.FullName);

        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(filename))
            .FailWith("You can't assert a file exist if you don't pass a proper name")
            .Then
            .Given(() => Subject.GetFiles())
            .ForCondition(files => files.All(fileInfo => !fileInfo.Name.Equals(filename)))
            .FailWith("Expected {context:directory} to not contain {0}{reason}, but the file exists.",
                _ => filename);

        return new AndConstraint<DirectoryInfoAssertions>(this);
    }
}
