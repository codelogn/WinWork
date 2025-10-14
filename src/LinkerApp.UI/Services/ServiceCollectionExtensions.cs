using Microsoft.Extensions.DependencyInjection;

namespace LinkerApp.UI.Services;

/// <summary>
/// Extension methods for registering UI services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all UI-specific services
    /// </summary>
    public static IServiceCollection AddLinkerAppUI(this IServiceCollection services)
    {
        // Register UI services here as we create them
        // services.AddSingleton<IThemeService, ThemeService>();
        // services.AddSingleton<ISystemTrayService, SystemTrayService>();
        // services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        
        return services;
    }
}