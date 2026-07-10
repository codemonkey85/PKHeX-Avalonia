using Microsoft.Extensions.DependencyInjection;
using PKHeX.Application.Abstractions;
using PKHeX.Infrastructure.Configuration;

namespace PKHeX.Infrastructure;

/// <summary>Registers the Infrastructure layer (non-UI drivers: file IO over Core).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISaveFileGateway, SaveFileService>();
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ISettingsStore, SettingsStore>();
        return services;
    }
}
