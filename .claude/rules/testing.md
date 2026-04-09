---
paths: ["**/*.Tests/**/*", "**/*Tests.cs", "**/*Spec.cs"]
---

## Testing conventions

- Unit tests: xUnit + FluentAssertions + NSubstitute
- Integration tests: use WebApplicationFactory, real SQL Server (TestContainers)
- Test class naming: {SystemUnderTest}Tests
- Test method naming: {Method}_{Scenario}_{ExpectedResult}
- Arrange/Act/Assert sections with blank line separating each
- Never mock the domain — test domain logic directly
- Mock only infrastructure boundaries (IRecipesDbContext, external HTTP clients)