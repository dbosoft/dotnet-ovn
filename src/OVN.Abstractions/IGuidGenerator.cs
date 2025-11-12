namespace Dbosoft.OVN;

/// <summary>
/// Defines a generator which can be used to generate the
/// UUIDs for new OVS database records.
/// </summary>
public interface IGuidGenerator
{
    /// <summary>
    /// Returns a new <see cref="Guid"/>.
    /// </summary>
    Guid GenerateGuid();
}
