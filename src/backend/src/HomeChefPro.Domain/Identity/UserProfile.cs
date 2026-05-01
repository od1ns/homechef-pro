using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Identity;

public sealed class UserProfile : AggregateRoot<Guid>
{
    public string FullName { get; private set; } = null!;
    public string? DefaultPhone { get; private set; }
    public string? DefaultAddress { get; private set; }
    public string PreferredLanguage { get; private set; } = "es-VE";
    public string? AvatarUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserProfile() { }

    private UserProfile(
        Guid userId,
        string fullName,
        string? defaultPhone,
        string? defaultAddress,
        string preferredLanguage,
        string? avatarUrl,
        DateTimeOffset now)
    {
        Id = userId;
        FullName = fullName;
        DefaultPhone = defaultPhone;
        DefaultAddress = defaultAddress;
        PreferredLanguage = preferredLanguage;
        AvatarUrl = avatarUrl;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static UserProfile Create(
        Guid userId,
        string fullName,
        string? defaultPhone = null,
        string? defaultAddress = null,
        string preferredLanguage = "es-VE",
        string? avatarUrl = null,
        TimeProvider? clock = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");
        if (fullName.Length > 160)
            throw new DomainException("Full name must be at most 160 characters.");
        if (string.IsNullOrWhiteSpace(preferredLanguage))
            throw new DomainException("Preferred language is required.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new UserProfile(
            userId,
            fullName.Trim(),
            string.IsNullOrWhiteSpace(defaultPhone) ? null : defaultPhone.Trim(),
            string.IsNullOrWhiteSpace(defaultAddress) ? null : defaultAddress.Trim(),
            preferredLanguage.Trim(),
            string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            now);
    }

    public void UpdateContactInfo(string? phone, string? address, TimeProvider? clock = null)
    {
        DefaultPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        DefaultAddress = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Touch(clock);
    }

    public void RenameTo(string fullName, TimeProvider? clock = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");
        if (fullName.Length > 160)
            throw new DomainException("Full name must be at most 160 characters.");
        FullName = fullName.Trim();
        Touch(clock);
    }

    public void SetPreferredLanguage(string language, TimeProvider? clock = null)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new DomainException("Preferred language is required.");
        PreferredLanguage = language.Trim();
        Touch(clock);
    }

    public void SetAvatar(string? avatarUrl, TimeProvider? clock = null)
    {
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        Touch(clock);
    }

    private void Touch(TimeProvider? clock) =>
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
}
