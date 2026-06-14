namespace PKHeX.Application.Abstractions;

/// <summary>
/// Shows a ViewModel as a modal dialog window. The host resolves the matching View via a ViewLocator,
/// so the Presentation layer never references View types.
/// </summary>
public interface IWindowService
{
    Task ShowDialogAsync(object viewModel, string title);
}
