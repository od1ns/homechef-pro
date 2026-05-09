using HomeChefPro.Application.Abstractions;
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
using HomeChefPro.Infrastructure.Identity;
using HomeChefPro.Infrastructure.Persistence.Naming;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Infrastructure.Persistence;

public sealed class HomeChefProDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>,
      IHomeChefProDbContext
{
    public HomeChefProDbContext(DbContextOptions<HomeChefProDbContext> options) : base(options) { }

    // Tenancy (multi-tenant root) — Pasada C / Fase 1C-A
    public DbSet<Chef> Chefs => Set<Chef>();

    // Catalog
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientPresentation> IngredientPresentations => Set<IngredientPresentation>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeComponent> RecipeComponents => Set<RecipeComponent>();
    public DbSet<RecipeModifier> RecipeModifiers => Set<RecipeModifier>();  // Etapa 2

    // Inventory
    public DbSet<IngredientPurchase> IngredientPurchases => Set<IngredientPurchase>();
    public DbSet<IngredientWaste> IngredientWaste => Set<IngredientWaste>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    // Exchange + Identity profile
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // Orders
    public DbSet<GuestCustomer> GuestCustomers => Set<GuestCustomer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();  // Etapa 2

    // Payments
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentProofUpload> PaymentProofUploads => Set<PaymentProofUpload>();  // F-23

    // Delivery
    public DbSet<DeliveryTracking> DeliveryTrackings => Set<DeliveryTracking>();
    public DbSet<DeliveryEvent> DeliveryEvents => Set<DeliveryEvent>();

    // Reviews
    public DbSet<Review> Reviews => Set<Review>();

    // Invoicing
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Customer preferences (onboarding sync)
    public DbSet<CustomerPreferences> CustomerPreferences => Set<CustomerPreferences>();

    // Refresh tokens (rotacion del JWT)
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Sesion A / Frente 1: codigos de invitacion para registro publico.
    public DbSet<InvitationCode> InvitationCodes => Set<InvitationCode>();
    public DbSet<InvitationCodeUse> InvitationCodeUses => Set<InvitationCodeUse>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(HomeChefProDbContext).Assembly);
        builder.ApplySnakeCaseNaming();
    }
}
