using Testcontainers.MySql;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// Shared MySQL 8 container for persistence integration tests (repository, lock, unique index, migration).
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    public MySqlContainer Container { get; } = new MySqlBuilder("mysql:8.0.36")
        .WithDatabase("elementum_test")
        .WithUsername("elementum")
        .WithPassword("elementum_test")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public Task InitializeAsync() => Container.StartAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();
}
