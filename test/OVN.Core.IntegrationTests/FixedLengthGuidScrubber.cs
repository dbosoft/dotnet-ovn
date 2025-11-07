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
        {
            return;
        }

        //{173535ae-995b-4cc6-a74e-8cd4be57039c}
        if (builder.Length < 36)
        {
            return;
        }

        var value = builder.ToString().AsSpan();

        var builderIndex = 0;
        for (var index = 0; index <= value.Length; index++)
        {
            var end = index + 36;
            if (end > value.Length)
            {
                return;
            }

            if ((index == 0 || !IsInvalidStartingChar(value[index - 1])) &&
                (end == value.Length || !IsInvalidEndingChar(value[end])))
            {
                var slice = value.Slice(index, 36);
                if (slice.IndexOfAny('\r', '\n') == -1 &&
                    Guid.TryParseExact(slice, "D", out var guid))
                {
                    var convert = counter.Next(guid);
                    builder.Remove(index, 36);
                    builder.Insert(index, $"Guid_{convert:0000000000000000000000000000000}");
                    builderIndex += 36;
                    index += 35;

                    continue;
                }
            }

            builderIndex++;
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
