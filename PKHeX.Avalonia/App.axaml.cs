using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PKHeX.Application;
using PKHeX.Infrastructure;
using PKHeX.Infrastructure.Configuration;
using PKHeX.Avalonia.Services;
using PKHeX.Presentation.ViewModels;
using PKHeX.Avalonia.Views;

namespace PKHeX.Avalonia;

public partial class App : global::Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize PKHeX Core
        // GameInfo is initialized via AppSettings in ConfigureServices


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Inner layers (framework-free): use cases, ports + app-side impls, non-UI drivers.
        services.AddApplication();
        services.AddInfrastructure();

        // Config: resolve platform paths, load (migrating/recovering as needed) + seed Core
        // localization, then register the store and the loaded model for injection.
        IAppPaths paths = new AppPaths();
        ISettingsStore settingsStore = new SettingsStore(paths);
        var config = settingsStore.Load();
        config.InitializeCore();
        services.AddSingleton(paths);
        services.AddSingleton(settingsStore);
        services.AddSingleton(config);

        // Host (Frameworks & Drivers): Avalonia/Skia implementations of the Application ports.
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ISpriteRenderer, AvaloniaSpriteRenderer>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();

        // Root ViewModel (transient). Child/editor ViewModels are created by their parent presenter
        // with the current save, so they are not registered in the container.
        services.AddTransient<MainWindowViewModel>();
    }
}
