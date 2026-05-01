using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Identity;

/// <summary>
/// JSONB blob of onboarding answers — the schema is intentionally a record so we can
/// forward-evolve fields without DB migrations. Each app posts the full object on save.
/// </summary>
public sealed class CustomerPreferences : AggregateRoot<Guid>
{
    public string PayloadJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; }

    private CustomerPreferences() { }

    public CustomerPreferences(Guid userId, string payloadJson, DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(payloadJson))
            payloadJson = "{}";
        Id = userId;
        PayloadJson = payloadJson;
        UpdatedAt = updatedAt;
    }

    public void Replace(string payloadJson, TimeProvider? clock = null)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            payloadJson = "{}";
        PayloadJson = payloadJson;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }
}
