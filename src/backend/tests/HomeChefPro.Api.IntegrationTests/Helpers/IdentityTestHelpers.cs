using System.Net.Http.Headers;
using System.Net.Http.Json;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Commands.LoginUser;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Helpers;

/// <summary>
/// Helpers para tests de integracion que necesitan crear users con roles privilegiados
/// (Admin, Cashier, Cook).
///
/// Contexto: el endpoint <c>POST /api/auth/register</c> es <c>AllowAnonymous</c> y, despues
/// del fix F-21 (audit Pasada A, BOPLA / API3:2023), ignora el campo <c>Roles</c> del body.
/// Es decir: cualquier registro via HTTP siempre crea un user con rol <c>Client</c>.
///
/// Para crear un Admin/Cashier/Cook en tests: registramos via HTTP, despues promovemos via
/// <see cref="IIdentityService.AssignRoleAsync"/> contra el <see cref="IServiceProvider"/>
/// del <see cref="WebApplicationFactory{TEntryPoint}"/>, y volvemos a hacer login para que
/// el JWT contenga los role claims actualizados.
/// </summary>
public static class IdentityTestHelpers
{
    public const string DefaultPassword = "Test1234";

    /// <summary>
    /// Registra un user via HTTP, lo promueve a los roles indicados via DI, y re-loguea
    /// para retornar un <see cref="AuthResultDto"/> con JWT que incluye los role claims.
    /// </summary>
    public static async Task<AuthResultDto> RegisterAndAssignRolesAsync(
        WebApplicationFactory<Program> factory,
        HttpClient client,
        string email,
        string password,
        string fullName,
        IReadOnlyCollection<string> roles,
        string? phone = null,
        string preferredLanguage = "es-VE")
    {
        // 1. Register via HTTP. El endpoint ignora request.Roles (F-21), siempre crea Client.
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(email, password, fullName, phone, preferredLanguage));
        if (!reg.IsSuccessStatusCode)
        {
            var body = await reg.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"RegisterAndAssignRolesAsync.register failed: got {(int)reg.StatusCode} {reg.StatusCode}. Body: {body}");
        }
        var registered = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;

        // Si solo se piden roles == Client, no hace falta promover ni re-loguear.
        var needsPromotion = roles.Count > 0
            && roles.Any(r => !string.Equals(r, Roles.Client, StringComparison.Ordinal));

        if (!needsPromotion)
            return registered;

        // 2. Promover via DI (no expuesto a HTTP anonymous).
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identity.EnsureRolesExistAsync(roles);
            foreach (var role in roles)
            {
                if (string.Equals(role, Roles.Client, StringComparison.Ordinal))
                    continue;
                var op = await identity.AssignRoleAsync(registered.UserId, role);
                if (!op.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"RegisterAndAssignRolesAsync.assignRole({role}) failed: {string.Join("; ", op.Errors)}");
                }
            }
        }

        // 3. Re-login para que el JWT contenga los role claims actualizados.
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginUserCommand(email, password));
        if (!login.IsSuccessStatusCode)
        {
            var body = await login.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"RegisterAndAssignRolesAsync.relogin failed: got {(int)login.StatusCode}. Body: {body}");
        }
        return (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    /// <summary>
    /// Conveniencia: registra + promueve + setea el Authorization header en el cliente.
    /// </summary>
    public static async Task<AuthResultDto> RegisterAndAuthenticateAsync(
        WebApplicationFactory<Program> factory,
        HttpClient client,
        string email,
        string password,
        string fullName,
        IReadOnlyCollection<string> roles,
        string? phone = null,
        string preferredLanguage = "es-VE")
    {
        var auth = await RegisterAndAssignRolesAsync(
            factory, client, email, password, fullName, roles, phone, preferredLanguage);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }
}
