using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Tenancy;

/// <summary>
/// Ciclo de vida del inquilino. Soft delete via Archived (datos preservados).
/// </summary>
public enum ChefStatus
{
    [DbValue("active")]    Active,
    [DbValue("suspended")] Suspended,
    [DbValue("archived")]  Archived,
}
