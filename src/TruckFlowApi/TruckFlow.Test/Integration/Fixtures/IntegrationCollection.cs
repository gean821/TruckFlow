namespace TruckFlow.Test.Integration.Fixtures;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Integration";
}
