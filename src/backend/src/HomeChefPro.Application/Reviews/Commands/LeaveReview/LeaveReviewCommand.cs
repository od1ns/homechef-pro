using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Reviews.Commands.LeaveReview;

public sealed record LeaveReviewCommand(
    Guid OrderId,
    Guid DishId,
    short Rating,
    string? Comment = null) : IRequest<Guid>;

public sealed class LeaveReviewValidator : AbstractValidator<LeaveReviewCommand>
{
    public LeaveReviewValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DishId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween((short)1, (short)5);
        RuleFor(x => x.Comment).MaximumLength(4000);
    }
}

public sealed class LeaveReviewHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<LeaveReviewCommand, Guid>
{
    public async Task<Guid> Handle(LeaveReviewCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.CustomerType != CustomerType.Registered || order.UserId != userId)
            throw new UnauthorizedAccessException(
                "Only the registered owner of the order may leave a review.");

        if (order.Status != OrderStatus.Delivered)
            throw new DomainException(
                $"Cannot review order {order.Id} until it is delivered. Current status: '{order.Status}'.");

        if (!order.Items.Any(i => i.DishId == request.DishId))
            throw new DomainException(
                $"Order {order.Id} does not contain dish {request.DishId}.");

        var duplicate = await db.Reviews.AnyAsync(r =>
            r.UserId == userId && r.OrderId == request.OrderId && r.DishId == request.DishId, ct)
            .ConfigureAwait(false);
        if (duplicate)
            throw new DomainException("You already reviewed this dish for this order.");

        var review = Review.Leave(userId, request.OrderId, request.DishId,
            request.Rating, request.Comment, clock);
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return review.Id;
    }
}
