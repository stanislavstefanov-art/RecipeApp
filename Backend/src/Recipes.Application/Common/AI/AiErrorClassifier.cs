namespace Recipes.Application.Common.AI;

public static class AiErrorClassifier
{
    public static AiErrorEnvelope Classify(Exception ex, string source)
    {
        var msg  = ex.Message;
        var code = ClassifyCode(msg);
        return new AiErrorEnvelope(
            Guid.NewGuid(),
            code,
            source,
            msg,
            IsRetryable: code is "api_error" or "timeout",
            OccurredAt: DateTime.UtcNow);
    }

    private static string ClassifyCode(string message)
    {
        if (ContainsAny(message, "api key", "missing"))           return "configuration_error";
        if (ContainsAny(message, "timed out", "timeout"))         return "timeout";
        if (ContainsAny(message, "deserializ", "json", "valid"))  return "output_validation";
        return "api_error";
    }

    private static bool ContainsAny(string message, params string[] terms)
        => terms.Any(t => message.Contains(t, StringComparison.OrdinalIgnoreCase));
}
