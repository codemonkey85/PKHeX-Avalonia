using System.Reflection;

namespace PKHeX.Presentation.ViewModels;

public class AboutViewModel
{
    public string UIVersion { get; }
    public string CoreVersion { get; }
    public string AvaloniaVersion { get; }

    public AboutViewModel()
    {
        // UI version comes from the host assembly (PKHeX.Avalonia); look it up by name so this
        // Presentation-layer type never references the host or the Avalonia framework directly.
        UIVersion = StripMeta(GetInformationalVersion("PKHeX.Avalonia")
                    ?? typeof(AboutViewModel).Assembly.GetName().Version?.ToString(3)
                    ?? "unknown");

        var coreAssembly = typeof(PKHeX.Core.PKM).Assembly;
        CoreVersion = coreAssembly.GetName().Version?.ToString(3) ?? "unknown";

        AvaloniaVersion = StripMeta(GetInformationalVersion("Avalonia.Base")
                          ?? GetInformationalVersion("Avalonia")
                          ?? "unknown");
    }

    private static string? GetInformationalVersion(string assemblyName)
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
        return asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? asm?.GetName().Version?.ToString(3);
    }

    private static string StripMeta(string version)
        => version.Contains('+') ? version[..version.IndexOf('+')] : version;
}
