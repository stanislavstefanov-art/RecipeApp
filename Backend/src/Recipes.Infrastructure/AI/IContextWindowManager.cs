using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI;

public interface IContextWindowManager
{
    // Trims messages in-place using keep-first / sliding-window.
    // Returns the number of messages dropped (0 if no trim needed).
    int Trim(List<ClaudeAgentMessage> messages, int maxMessages);
}
