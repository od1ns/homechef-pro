namespace HomeChefPro.Application.Abstractions;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string Cook = "Cook";
    public const string Client = "Client";

    public static IReadOnlyCollection<string> All { get; } = [Admin, Cashier, Cook, Client];
}
