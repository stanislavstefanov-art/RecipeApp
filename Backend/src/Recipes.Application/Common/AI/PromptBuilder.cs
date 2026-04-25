using System.Text;

namespace Recipes.Application.Common.AI;

public sealed record PromptExample(string Description, string Input, string Output);

public sealed class PromptBuilder
{
    private string? _role;
    private string? _inputSpec;
    private string? _outputSchema;
    private IReadOnlyList<string> _successCriteria = [];
    private IReadOnlyList<string> _failureModes = [];
    private IReadOnlyList<PromptExample> _examples = [];

    public PromptBuilder WithRole(string role) { _role = role; return this; }
    public PromptBuilder WithInputSpec(string spec) { _inputSpec = spec; return this; }
    public PromptBuilder WithOutputSchema(string schema) { _outputSchema = schema; return this; }
    public PromptBuilder WithSuccessCriteria(params string[] criteria) { _successCriteria = criteria; return this; }
    public PromptBuilder WithFailureModes(params string[] modes) { _failureModes = modes; return this; }
    public PromptBuilder WithExamples(params PromptExample[] examples) { _examples = examples; return this; }

    public string Build()
    {
        var sb = new StringBuilder();

        AppendSection(sb, "ROLE", _role);
        AppendSection(sb, "INPUT SPECIFICATION", _inputSpec);
        AppendSection(sb, "OUTPUT SCHEMA", _outputSchema);
        AppendListSection(sb, "SUCCESS CRITERIA", _successCriteria);
        AppendListSection(sb, "FAILURE MODES", _failureModes);
        AppendExamplesSection(sb, _examples);

        return sb.ToString().TrimEnd();
    }

    private static void AppendSection(StringBuilder sb, string heading, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        sb.AppendLine($"## {heading}");
        sb.AppendLine(content);
        sb.AppendLine();
    }

    private static void AppendListSection(StringBuilder sb, string heading, IReadOnlyList<string> items)
    {
        if (items.Count == 0) return;
        sb.AppendLine($"## {heading}");
        foreach (var item in items)
            sb.AppendLine($"- {item}");
        sb.AppendLine();
    }

    private static void AppendExamplesSection(StringBuilder sb, IReadOnlyList<PromptExample> examples)
    {
        if (examples.Count == 0) return;
        sb.AppendLine("## EXAMPLES");
        for (var i = 0; i < examples.Count; i++)
        {
            var ex = examples[i];
            sb.AppendLine($"### Example {i + 1}: {ex.Description}");
            sb.AppendLine("**Input:**");
            sb.AppendLine(ex.Input);
            sb.AppendLine("**Output:**");
            sb.AppendLine(ex.Output);
            sb.AppendLine();
        }
    }
}
