namespace Elementum.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
{
    public const string Name = "MySql";
}
