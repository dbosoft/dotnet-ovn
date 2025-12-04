// Based on https://github.com/VerifyTests/Verify/blob/main/src/Verify/Serialization/Scrubbers/GuidScrubber.cs
// Copyright(c).NET Foundation and Contributors
// Licensed under the MIT License.
using System.Text;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class FixedLengthHashScrubber
{
    public static void ReplaceHashes(StringBuilder builder)
    {
        // Note that the counter is not tracked inside Verify's state.
        // This works for our use case but is not a general solution.
        var cache = new Dictionary<string, string>();
        long counter = 1;

        if (builder.Length < 64)
            return;

        var value = builder.ToString().AsSpan();

        for (var index = 0; index < value.Length - 64; index++)
        {
            var slice = value.Slice(index, 64).ToString();
            if (!slice.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                continue;
            
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
    }
}
