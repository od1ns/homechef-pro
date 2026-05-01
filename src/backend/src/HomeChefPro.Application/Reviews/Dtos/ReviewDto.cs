namespace HomeChefPro.Application.Reviews.Dtos;

public sealed record ReviewDto(
    Guid Id,
    Guid UserId,
    Guid OrderId,
    Guid DishId,
    short Rating,
    string? Comment,
    bool IsVisible,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Public-facing review: the customer's name is masked to initials so other clients
/// see something like "María F." without exposing full identity.
/// </summary>
public sealed record PublicReviewDto(
    Guid Id,
    Guid DishId,
    short Rating,
    string? Comment,
    string CustomerDisplay,    // "María F.", "Invitado", etc.
    DateTimeOffset CreatedAt);
