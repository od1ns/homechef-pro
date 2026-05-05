using HomeChefPro.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeChefPro.Infrastructure.Persistence;

public static class EnumPropertyBuilderExtensions
{
    /// <summary>
    /// Serializes a C# enum as its <c>[DbValue("...")]</c> string (matching the SQL CHECK constraints).
    /// </summary>
    public static PropertyBuilder<TEnum> HasEnumDbValueConversion<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum
    {
        var converter = new ValueConverter<TEnum, string>(
            v => EnumDbMap<TEnum>.ToDb(v),
            s => EnumDbMap<TEnum>.FromDb(s));
        return builder.HasConversion(converter);
    }

    public static PropertyBuilder<TEnum?> HasEnumDbValueConversion<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum
    {
        var converter = new ValueConverter<TEnum?, string?>(
            v => v.HasValue ? EnumDbMap<TEnum>.ToDb(v.Value) : null,
            s => s == null ? null : EnumDbMap<TEnum>.FromDb(s));
        return builder.HasConversion(converter);
    }

    /// <summary>
    /// F-26 (Tier 2): activa optimistic concurrency usando la columna <c>xmin</c>
    /// que Postgres mantiene automaticamente en cada row (transaction id de la
    /// ultima escritura). EF Core + Npgsql comparan el xmin en cada UPDATE: si
    /// la row cambio entre el SELECT y el UPDATE, la operacion afecta 0 filas
    /// y EF lanza <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>.
    ///
    /// Uso: <code>builder.UseXminConcurrencyToken();</code> en la configuracion
    /// de cualquier entity con riesgo de updates concurrentes.
    /// </summary>
    public static Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T>
        UseXminConcurrencyToken<T>(
            this Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        return builder;
    }
}
