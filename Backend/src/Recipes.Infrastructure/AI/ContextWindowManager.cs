using Microsoft.Extensions.Logging;
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI;

public sealed class ContextWindowManager : IContextWindowManager
{
    private readonly ILogger<ContextWindowManager> _logger;

    public ContextWindowManager(ILogger<ContextWindowManager> logger)
        => _logger = logger;

    public int Trim(List<ClaudeAgentMessage> messages, int maxMessages)
    {
        if (messages.Count <= maxMessages)
            return 0;

        // Keep messages[0] (original task description — never dropped) plus the
        // most recent windowSize messages. Drop everything in between.
        var windowSize  = maxMessages / 2;
        var keepFromIdx = messages.Count - windowSize;
        var dropCount   = keepFromIdx - 1;

        if (dropCount <= 0)
            return 0;

        messages.RemoveRange(1, dropCount);

        _logger.LogWarning(
            "ContextWindowManager: dropped {Dropped} messages to stay within " +
            "MaxContextMessages={Max}. Remaining: {Remaining}.",
            dropCount, maxMessages, messages.Count);

        return dropCount;
    }
}
