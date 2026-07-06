using System;
using System.IO;
using CommunityToolkit.Mvvm.Input;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// Dialog showing a QR code for a Pokémon, with save-to-PNG. The image is rendered by the
/// Infrastructure QR service; this VM only carries the PNG bytes and a caption.
/// </summary>
public partial class QrCodeViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;

    public byte[] PngBytes { get; }
    public string Caption { get; }
    public string DefaultFileName { get; }

    public QrCodeViewModel(byte[] pngBytes, string caption, string defaultFileName, IDialogService dialogService)
    {
        PngBytes = pngBytes;
        Caption = caption;
        DefaultFileName = defaultFileName;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveAsPngAsync()
    {
        var path = await _dialogService.SaveFileAsync("Save QR Code", DefaultFileName);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.WriteAllBytes(path, PngBytes);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Save Error", ex.Message);
        }
    }
}
