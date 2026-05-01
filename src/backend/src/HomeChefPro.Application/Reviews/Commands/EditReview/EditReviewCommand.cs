using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Reviews;
using MediatR;

namespace HomeChefPro.Application.Reviews.Commands.EditReview;

public sealed record EditReviewCommand(Guid ReviewId, short Rating, string? Comment = null) : IRequest;

public sealed class EditReviewValidator : AbstractValidator<EditReviewCommand>
{
    public EditReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween((short)1, (short)5);
        RuleFor(x => x.Comment).MaximumLength(4000);
    }
}

public sealed class EditReviewHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<EditReviewCommand>
{
    public async Task Handle(EditReviewCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var review = await db.Reviews.FindAsync([request.ReviewId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Review), request.ReviewId);

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Only the author can edit this review.");

        review.Edit(request.Rating, request.Comment, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
