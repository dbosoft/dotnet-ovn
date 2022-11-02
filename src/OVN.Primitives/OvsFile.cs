namespace Dbosoft.OVN;

public record OvsFile(string Path, string Name, bool IsExe = false)
{
    public readonly bool IsExe = IsExe;
    public readonly string Name = Name;
    public readonly string Path = Path;
}