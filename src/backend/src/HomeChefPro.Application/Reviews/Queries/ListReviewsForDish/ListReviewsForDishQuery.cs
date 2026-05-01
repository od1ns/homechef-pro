using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Reviews.Dtos;
using HomeChefPro.Application.Reviews.Mapping;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Reviews.Queries.ListReviewsForDish;

public sealed record ListReviewsForDishQuery(Guid DishId, int Take = 50) : IRequest<IReadOnlyList<PublicReviewDto>>;

public sealed class ListReviewsForDishHandler(IHomeChefProDbContext db)
    : IRequestHandler<ListReviewsForDishQuery, IReadOnlyList<PublicReviewDto>>
{
    public async Task<IReadOnlyList<PublicReviewDto>> Handle(
        ListReviewsForDishQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 200);

        // Join review→user_profiles to resolve the display name. We select the raw fields to
        // avoid tracking and to be explicit about the projection.
        var rows = await db.Reviews.AsNoTracking()
            .Where(r => r.DishId == request.DishId && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .Join(db.UserProfiles.AsNoTracking(),
                r => r.UserId,
                p => p.Id,
                (r, p) => new { Review = r, Profile = (string?)p.FullName })
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(r => r.Review.ToPublic(r.Profile)).ToArray();
    }
}
