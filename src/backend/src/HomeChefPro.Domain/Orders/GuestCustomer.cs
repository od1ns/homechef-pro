using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public sealed class GuestCustomer : AggregateRoot<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public string FullName { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private GuestCustomer() { }

    private GuestCustomer(Guid id, string fullName, string phone, DateTimeOffset createdAt)
    {
        Id = id;
        FullName = fullName;
        Phone = phone;
        CreatedAt = createdAt;
    }

    public static GuestCustomer Create(
        string fullName,
        string phone,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");
        if (fullName.Length > 160)
            throw new DomainException("Full name must be at most 160 characters.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("Phone is required.");
        if (phone.Length > 30)
            throw new DomainException("Phone must be at most 30 characters.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new GuestCustomer(id ?? Guid.NewGuid(), fullName.Trim(), phone.Trim(), now);
    }
}
