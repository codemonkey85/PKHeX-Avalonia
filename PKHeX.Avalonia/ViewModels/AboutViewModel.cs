using System.Reflection;

namespace PKHeX.Avalonia.ViewModels;

public class AboutViewModel
{
    public string UIVersion { get; }
    public string CoreVersion { get; }
    public string AvaloniaVersion { get; }

    public AboutViewModel()
    {
        // Read UI version from PKHeX.Avalonia assembly
        var uiAssembly = typeof(AboutViewModel).Assembly;
        UIVersion = uiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? uiAssembly.GetName().Version?.ToString(3)
                    ?? "unknown";

        // Read Core version from PKHeX.Core assembly
        var coreAssembly = typeof(PKHeX.Core.PKM).Assembly;
        CoreVersion = coreAssembly.GetName().Version?.ToString(3) ?? "unknown";

        // Read Avalonia version from Avalonia assembly
        var avaloniaAssembly = typeof(global::Avalonia.Application).Assembly;
        AvaloniaVersion = avaloniaAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? avaloniaAssembly.GetName().Version?.ToString(3)
                          ?? "unknown";

        // Strip any +metadata suffix from informational versions
        if (UIVersion.Contains('+'))
            UIVersion = UIVersion[..UIVersion.IndexOf('+')];
        if (AvaloniaVersion.Contains('+'))
            AvaloniaVersion = AvaloniaVersion[..AvaloniaVersion.IndexOf('+')];
    }
}
