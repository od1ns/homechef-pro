using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Reviews.Dtos;
using HomeChefPro.Application.Reviews.Mapping;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Reviews.Queries.ListMyReviews;

public sealed record ListMyReviewsQuery : IRequest<IReadOnlyList<ReviewDto>>;

public sealed class ListMyReviewsHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<ListMyReviewsQuery, IReadOnlyList<ReviewDto>>
{
    public async Task<IReadOnlyList<ReviewDto>> Handle(ListMyReviewsQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var rows = await db.Reviews.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(r => r.ToDto()).ToArray();
    }
}
