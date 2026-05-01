using HomeChefPro.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Customers.Queries.GetMyPreferences;

public sealed record GetMyPreferencesQuery : IRequest<MyPreferencesDto>;

public sealed record MyPreferencesDto(
    string PayloadJson,
    DateTimeOffset? UpdatedAt);

public sealed class GetMyPreferencesHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyPreferencesQuery, MyPreferencesDto>
{
    public async Task<MyPreferencesDto> Handle(GetMyPreferencesQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var row = await db.CustomerPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, ct)
            .ConfigureAwait(false);
        return row is null
            ? new MyPreferencesDto("{}", null)
            : new MyPreferencesDto(row.PayloadJson, row.UpdatedAt);
    }
}
