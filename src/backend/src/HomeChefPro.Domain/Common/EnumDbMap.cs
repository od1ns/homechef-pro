using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HomeChefPro.Domain.Common;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Per-closed-generic static maps are the whole point of this helper.")]
public static class EnumDbMap<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> _toDb;
    private static readonly Dictionary<string, TEnum> _fromDb;

    static EnumDbMap()
    {
        var type = typeof(TEnum);
        _toDb = Enum.GetValues<TEnum>().ToDictionary(
            e => e,
            e =>
            {
                var field = type.GetField(e.ToString(), BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"Field not found: {type.Name}.{e}");
                var attr = field.GetCustomAttribute<DbValueAttribute>()
                    ?? throw new InvalidOperationException(
                        $"Missing [DbValue] on {type.Name}.{e}");
                return attr.Value;
            });

        _fromDb = new Dictionary<string, TEnum>(StringComparer.Ordinal);
        foreach (var (k, v) in _toDb)
        {
            _fromDb[v] = k;
        }
    }

    public static string ToDb(TEnum value) =>
        _toDb.TryGetValue(value, out var s)
            ? s
            : throw new ArgumentOutOfRangeException(nameof(value), value, null);

    public static TEnum FromDb(string value) =>
        _fromDb.TryGetValue(value, out var e)
            ? e
            : throw new ArgumentException(
                $"Unknown {typeof(TEnum).Name} DB value: '{value}'", nameof(value));

    public static bool TryFromDb(string value, out TEnum result) =>
        _fromDb.TryGetValue(value, out result);
}
