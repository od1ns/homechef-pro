using HomeChefPro.Domain.Common;
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
}
