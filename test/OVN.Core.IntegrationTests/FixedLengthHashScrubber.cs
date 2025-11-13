using System.Text;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class FixedLengthHashScrubber
{
    public static void ReplaceHashes(StringBuilder builder)
    {
        var cache = new Dictionary<string, string>();
        long counter = 1;

        if (builder.Length < 64)
            return;

        var value = builder.ToString().AsSpan();

        for (var index = 0; index < value.Length - 64; index++)
        {
            var slice = value.Slice(index, 64).ToString();
            if (slice.All(c => c is >='0' and <='9' or >= 'a' and <='f'))
            {
                if (!cache.TryGetValue(slice, out var counterValue))
                {
                    counterValue = $"SHA256_{counter:000000000000000000000000000000000000000000000000000000000}";
                    cache.Add(slice, counterValue);
                    counter++;
                }

                builder.Remove(index, 64);
                builder.Insert(index, counterValue);
                index += 63;
            }

            index++;
        }
    }
}
