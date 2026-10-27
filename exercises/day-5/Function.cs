using System;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLearning.Day5.Lambda;

public sealed record SummaryResponse(int WordCount, int CharacterCount, string Preview);

public sealed class Function
{
    public SummaryResponse FunctionHandler(string input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        int words = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        int previewLength = Math.Min(input.Length, 100);

        // Avoid splitting a UTF-16 surrogate pair at the preview boundary.
        if (previewLength < input.Length && previewLength > 0
            && char.IsHighSurrogate(input[previewLength - 1])
            && char.IsLowSurrogate(input[previewLength]))
        {
            previewLength--;
        }

        context.Logger.LogLine($"Processed text containing {words} words.");
        return new SummaryResponse(words, input.Length, input[..previewLength]);
    }
}
