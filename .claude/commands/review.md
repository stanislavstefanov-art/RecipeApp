Review the staged changes in this repository.

For each changed file:
- Check for violations of the architecture rules in CLAUDE.md
- Flag any domain logic leaking into Application or Infrastructure layers
- Flag any direct DbContext usage outside Infrastructure
- Flag hardcoded strings, connection strings, or API keys
- Check that new commands have corresponding validators

Report findings as: [FILE] [SEVERITY: high/medium/low] [ISSUE] [SUGGESTED FIX]
Only report genuine issues — do not flag style preferences or local patterns.