using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Reviews;

public sealed class Review : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid DishId { get; private set; }

    public short Rating { get; private set; }
    public string? Comment { get; private set; }

    public bool IsVisible { get; private set; }
    public Guid? ModeratedBy { get; private set; }
    public DateTimeOffset? ModeratedAt { get; private set; }
    public string? ModerationNote { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Review() { }

    private Review(
        Guid id,
        Guid userId,
        Guid orderId,
        Guid dishId,
        short rating,
        string? comment,
        DateTimeOffset now)
    {
        Id = id;
        UserId = userId;
        OrderId = orderId;
        DishId = dishId;
        Rating = rating;
        Comment = comment;
        IsVisible = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Review Leave(
        Guid userId,
        Guid orderId,
        Guid dishId,
        short rating,
        string? comment = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (userId == Guid.Empty) throw new DomainException("UserId is required.");
        if (orderId == Guid.Empty) throw new DomainException("OrderId is required.");
        if (dishId == Guid.Empty) throw new DomainException("DishId is required.");
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Review(
            id ?? Guid.NewGuid(),
            userId,
            orderId,
            dishId,
            rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            now);
    }

    public void Edit(short rating, string? comment, TimeProvider? clock = null)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5.");
        Rating = rating;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void Hide(Guid moderatorId, string? note = null, TimeProvider? clock = null)
    {
        if (moderatorId == Guid.Empty)
            throw new DomainException("ModeratorId is required.");
        IsVisible = false;
        ModeratedBy = moderatorId;
        ModerationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ModeratedAt = (clock ?? TimeProvider.System).GetUtcNow();
        UpdatedAt = ModeratedAt.Value;
    }

    public void Restore(Guid moderatorId, string? note = null, TimeProvider? clock = null)
    {
        if (moderatorId == Guid.Empty)
            throw new DomainException("ModeratorId is required.");
        IsVisible = true;
        ModeratedBy = moderatorId;
        ModerationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ModeratedAt = (clock ?? TimeProvider.System).GetUtcNow();
        UpdatedAt = ModeratedAt.Value;
    }
}
