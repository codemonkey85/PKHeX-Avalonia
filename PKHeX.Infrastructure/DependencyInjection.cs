using Microsoft.Extensions.DependencyInjection;
using PKHeX.Application.Abstractions;

namespace PKHeX.Infrastructure;

/// <summary>Registers the Infrastructure layer (non-UI drivers: file IO over Core).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISaveFileGateway, SaveFileService>();
        return services;
    }
}
