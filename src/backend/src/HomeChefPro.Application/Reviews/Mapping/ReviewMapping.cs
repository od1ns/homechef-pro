using HomeChefPro.Application.Reviews.Dtos;
using HomeChefPro.Domain.Reviews;

namespace HomeChefPro.Application.Reviews.Mapping;

public static class ReviewMapping
{
    public static ReviewDto ToDto(this Review r) =>
        new(r.Id, r.UserId, r.OrderId, r.DishId, r.Rating, r.Comment,
            r.IsVisible, r.CreatedAt, r.UpdatedAt);

    public static PublicReviewDto ToPublic(this Review r, string? fullName) =>
        new(r.Id, r.DishId, r.Rating, r.Comment,
            CustomerDisplay: ShortName(fullName),
            CreatedAt: r.CreatedAt);

    private static string ShortName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Cliente";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];
        return $"{parts[0]} {parts[1][..1].ToUpperInvariant()}.";
    }
}
