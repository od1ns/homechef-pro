using System.Reflection;
using FluentValidation;
using HomeChefPro.Application.Auth.Services;
using HomeChefPro.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        // AutoMapper removido (NU1903 GHSA-rvv3-g6hj-g44x). El proyecto usa
        // plain mapping functions explicitas en *.Mapping/ y nunca inyectaba
        // IMapper en ningun handler.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<RefreshTokenIssuer>();

        return services;
    }
}
