namespace TronderLeikan.Api.Tests;

// Alle test-klasser som bruker [Collection(nameof(ApiTestCollection))]
// deler én factory (og én database) per testkjøring
[CollectionDefinition(nameof(ApiTestCollection))]
public class ApiTestCollection : ICollectionFixture<TronderLeikanApiFactory> { }
