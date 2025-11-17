using IdGen;

namespace Dbosoft.OVN.Core.IntegrationTests;

/// <summary>
/// This generator creates GUIDs which are strictly monotonically increasing.
/// </summary>
/// <remarks>
/// The generated GUIDs are still marked as version 4 GUIDs, but they are violating
/// both the randomness and the unpredictability guarantees which are provided by GUIDs.
/// This generator should only be used for testing purposes.
/// </remarks>
public class CombGuidGenerator : IGuidGenerator
{
    private static readonly DateTimeOffset Epoch = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TickDuration = TimeSpan.FromMilliseconds(1);
    private static readonly IdStructure IdStructure = new(41, 10, 12);

    private static readonly ITimeSource TimeSource = new DefaultTimeSource(Epoch, TickDuration);
    private static readonly IdGenerator IdGenerator = new(
        0,
        new IdGeneratorOptions(IdStructure, TimeSource, SequenceOverflowStrategy.SpinWait));

    public Guid GenerateGuid()
    {
        Span<byte> idSpan = stackalloc byte[8];
        var id = IdGenerator.CreateId();

        // The ID is written in little-endian format (assuming a little-endian system)
        BitConverter.TryWriteBytes(idSpan, id);

        Span<byte> guidSpan = stackalloc byte[16];

        idSpan[4..8].CopyTo(guidSpan[..4]);
        guidSpan[7] = 0x40;
        guidSpan[8] = 0x80;

        // These bytes of the GUID are in big-endian. Hence, we reverse
        // the bytes after copying them from the little-endian ID.
        idSpan[..4].CopyTo(guidSpan[12..16]);
        guidSpan[12..16].Reverse();

        return new Guid(guidSpan);
    }
}
