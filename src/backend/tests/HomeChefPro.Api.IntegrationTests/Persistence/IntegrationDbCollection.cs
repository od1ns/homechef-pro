namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Collection definition para que TODAS las clases de tests integration
/// compartan UN solo <see cref="LiveDatabaseFixture"/>. Esto:
///
///  1. Levanta UN unico contenedor Postgres (no uno por clase de test).
///  2. Serializa la ejecucion entre clases de la misma collection (xunit
///     corre tests en paralelo solo entre clases de DISTINTAS collections).
///
/// Antes: 11 clases con IClassFixture levantaban 11 contenedores en paralelo
/// y el runner ubuntu-latest reventaba (Connection refused, timeouts).
/// </summary>
[CollectionDefinition("IntegrationDb")]
public sealed class IntegrationDbCollection : ICollectionFixture<LiveDatabaseFixture>
{
    // Sin codigo. xunit usa esta clase solo como marca para enlazar
    // CollectionDefinition con el fixture.
}
