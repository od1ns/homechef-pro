using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Reviews;
using MediatR;

namespace HomeChefPro.Application.Reviews.Commands.ModerateReview;

public sealed record HideReviewCommand(Guid ReviewId, string? Note = null) : IRequest;

public sealed class HideReviewValidator : AbstractValidator<HideReviewCommand>
{
    public HideReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(4000);
    }
}

public sealed class HideReviewHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<HideReviewCommand>
{
    public async Task Handle(HideReviewCommand request, CancellationToken ct)
    {
        var moderatorId = currentUser.RequireUserId();
        var review = await db.Reviews.FindAsync([request.ReviewId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Review), request.ReviewId);
        review.Hide(moderatorId, request.Note, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed record RestoreReviewCommand(Guid ReviewId, string? Note = null) : IRequest;

public sealed class RestoreReviewValidator : AbstractValidator<RestoreReviewCommand>
{
    public RestoreReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(4000);
    }
}

public sealed class RestoreReviewHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<RestoreReviewCommand>
{
    public async Task Handle(RestoreReviewCommand request, CancellationToken ct)
    {
        var moderatorId = currentUser.RequireUserId();
        var review = await db.Reviews.FindAsync([request.ReviewId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Review), request.ReviewId);
        review.Restore(moderatorId, request.Note, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
