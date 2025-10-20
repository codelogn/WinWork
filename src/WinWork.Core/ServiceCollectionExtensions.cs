using Microsoft.Extensions.DependencyInjection;
using WinWork.Core.Services;
using WinWork.Core.Interfaces;

namespace WinWork.Core;

/// <summary>
/// Extension methods for registering core services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all core business logic services
    /// </summary>
    public static IServiceCollection AddWinWorkCore(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ILinkService, LinkService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ILinkOpenerService, LinkOpenerService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddSingleton<ISystemTrayService, SystemTrayService>();
        services.AddSingleton<IGlobalHotkeysService, GlobalHotkeysService>();

        return services;
    }
}
