// Based on https://github.com/VerifyTests/Verify/blob/main/src/Verify/Serialization/Scrubbers/GuidScrubber.cs
// Copyright(c).NET Foundation and Contributors
// Licensed under the MIT License.
using System.Text;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class FixedLengthGuidScrubber
{
    public static void ReplaceGuids(StringBuilder builder, Counter counter)
    {
        if (!counter.ScrubGuids)
            return;
        
        if (builder.Length < 36)
            return;
        
        var value = builder.ToString().AsSpan();
        
        for (var index = 0; index <= value.Length; index++)
        {
            var end = index + 36;
            if (end > value.Length)
                return;

            if (index != 0 && IsInvalidStartingChar(value[index - 1])
                || end != value.Length && IsInvalidEndingChar(value[end]))
            {
                continue;
            }
            var slice = value.Slice(index, 36);
            if (slice.IndexOfAny('\r', '\n') != -1 || !Guid.TryParseExact(slice, "D", out var guid))
                continue;
            
            var convert = counter.Next(guid);
            builder.Remove(index, 36);
            builder.Insert(index, $"Guid_{convert:0000000000000000000000000000000}");
            index += 35;
        }
    }

    static bool IsInvalidEndingChar(char ch) =>
        IsInvalidChar(ch) &&
        ch != '}' &&
        ch != ')';

    static bool IsInvalidChar(char ch) =>
        char.IsLetter(ch) ||
        char.IsNumber(ch);

    static bool IsInvalidStartingChar(char ch) =>
        IsInvalidChar(ch) &&
        ch != '{' &&
        ch != '(';
}
