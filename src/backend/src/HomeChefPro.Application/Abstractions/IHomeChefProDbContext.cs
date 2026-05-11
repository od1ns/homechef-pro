using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Delivery;
using HomeChefPro.Domain.Exchange;
using HomeChefPro.Domain.Identity;
using HomeChefPro.Domain.Invitations;
using HomeChefPro.Domain.Inventory;
using HomeChefPro.Domain.Invoicing;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using HomeChefPro.Domain.Reviews;
using HomeChefPro.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Abstractions;

/// <summary>
/// The Application layer's view onto the database. Infrastructure implements this via EF Core.
/// Handlers query DbSets directly with LINQ — this avoids a zoo of per-aggregate repositories.
/// </summary>
public interface IHomeChefProDbContext
{
    // Tenancy — Pasada C / Fase 1C-A
    DbSet<Chef> Chefs { get; }

    DbSet<Ingredient> Ingredients { get; }
    DbSet<IngredientPresentation> IngredientPresentations { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeComponent> RecipeComponents { get; }
    DbSet<RecipeModifier> RecipeModifiers { get; }  // Etapa 2

    DbSet<IngredientPurchase> IngredientPurchases { get; }
    DbSet<IngredientWaste> IngredientWaste { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }

    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<UserProfile> UserProfiles { get; }

    DbSet<GuestCustomer> GuestCustomers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderItemModifier> OrderItemModifiers { get; }  // Etapa 2
    DbSet<OrderDeviceToken> OrderDeviceTokens { get; }   // Etapa 5

    DbSet<Payment> Payments { get; }
    DbSet<PaymentProofUpload> PaymentProofUploads { get; }  // F-23

    DbSet<DeliveryTracking> DeliveryTrackings { get; }
    DbSet<DeliveryEvent> DeliveryEvents { get; }

    DbSet<Review> Reviews { get; }

    DbSet<Invoice> Invoices { get; }

    DbSet<CustomerPreferences> CustomerPreferences { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    // Sesion A / Frente 1
    DbSet<InvitationCode> InvitationCodes { get; }
    DbSet<InvitationCodeUse> InvitationCodeUses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
