namespace HomeChefPro.Domain.Common;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DbValueAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}
