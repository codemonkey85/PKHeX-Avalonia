using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PKHeX.Application.Abstractions;

namespace PKHeX.Avalonia.Services;

/// <summary>
/// Shows a ViewModel as a modal dialog. The matching View is resolved via <see cref="ViewLocator"/>,
/// wrapped in a host <c>Window</c>, and shown over the main window — preserving the previous
/// modal/centered behavior while keeping View resolution out of the Presentation layer.
/// </summary>
public sealed class WindowService : IWindowService
{
    public async Task ShowDialogAsync(object viewModel, string title)
    {
        var owner = (global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null) return;

        var dialog = new Window
        {
            Title = title,
            Content = ViewLocator.Build(viewModel),
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            MaxWidth = 1400,
            MaxHeight = 820,
        };

        if (viewModel is ICloseableDialog closeable)
            closeable.CloseRequested = dialog.Close;

        await dialog.ShowDialog(owner);
    }
}
