namespace HomeChefPro.Api.Endpoints;

internal static class EndpointResults
{
    public static IResult CreatedId(string location, Guid id) =>
        Results.Created(location, new { id });
}
