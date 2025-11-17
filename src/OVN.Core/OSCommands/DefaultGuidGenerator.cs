namespace Dbosoft.OVN.OSCommands;

public class DefaultGuidGenerator : IGuidGenerator
{
    public Guid GenerateGuid() => Guid.NewGuid();
}
