using Microsoft.Extensions.DependencyInjection;
using PKHeX.Application.Services;

namespace PKHeX.Application;

/// <summary>Registers the Application layer (use cases + framework-free app services).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<LanguageService>();
        services.AddSingleton<UndoRedoService>();
        // Use cases are registered here in Phase 5.
        return services;
    }
}
