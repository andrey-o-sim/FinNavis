namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

internal sealed record RecordedCommand(
    string Text,
    IReadOnlyList<KeyValuePair<string, object?>> Parameters);
