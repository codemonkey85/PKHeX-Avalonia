using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Application.Abstractions;
using PKHeX.Application.Abstractions.LiveHex;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// LiveHeX tool window: connect to a hacked console over Wi-Fi (sys-botbase TCP) and read/write the
/// currently displayed box. Framework-free; all network IO is delegated to <see cref="ILiveHexService"/>
/// and all writes are gated behind an explicit confirmation dialog.
/// </summary>
public partial class LiveHeXViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly ILiveHexService _liveHex;
    private readonly IDialogService _dialogs;
    private readonly Func<int> _getCurrentBox;
    private readonly Action _onBoxUpdated;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _ipAddress = "192.168.0.1";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private int _port = 6000;

    [ObservableProperty] private string _status;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadBoxCommand))]
    [NotifyCanExecuteChangedFor(nameof(WriteBoxCommand))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadBoxCommand))]
    [NotifyCanExecuteChangedFor(nameof(WriteBoxCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _sessionInfo = string.Empty;

    public LiveHeXViewModel(
        SaveFile sav,
        ILiveHexService liveHex,
        IDialogService dialogs,
        Func<int> getCurrentBox,
        Action onBoxUpdated)
    {
        _sav = sav;
        _liveHex = liveHex;
        _dialogs = dialogs;
        _getCurrentBox = getCurrentBox;
        _onBoxUpdated = onBoxUpdated;

        var support = _liveHex.GetSupport(sav);
        GameName = support.GameName;
        IsGameSupported = support.Supported;
        SupportDetail = support.Detail;
        _status = support.Supported
            ? "Ready. Enter the console IP and connect."
            : $"{support.GameName} is not supported by LiveHeX.";
    }

    /// <summary>Loaded game's display name.</summary>
    public string GameName { get; }

    /// <summary>Whether LiveHeX supports the loaded save type.</summary>
    public bool IsGameSupported { get; }

    /// <summary>Support note (supported firmware versions, or why unsupported).</summary>
    public string SupportDetail { get; }

    /// <summary>Static support matrix shown in the tool window.</summary>
    public string SupportMatrix =>
        "Sword/Shield — 1.1.1, 1.2.1, 1.3.2\n" +
        "Brilliant Diamond/Shining Pearl — 1.0.0–1.3.0\n" +
        "Legends: Arceus — 1.0.0, 1.0.1, 1.0.2, 1.1.1\n" +
        "Scarlet/Violet — 1.0.1 through 4.0.0\n\n" +
        "Connection: sys-botbase over Wi-Fi (TCP, default port 6000). USB is not supported in this version.";

    private bool CanConnect => IsGameSupported && !IsConnected && !IsBusy
                               && !string.IsNullOrWhiteSpace(IpAddress) && Port is > 0 and <= 65535;
    private bool CanDisconnect => IsConnected && !IsBusy;
    private bool CanTransfer => IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsBusy = true;
        Status = $"Connecting to {IpAddress}:{Port}…";
        try
        {
            await _liveHex.ConnectAsync(IpAddress, Port, _sav);
            IsConnected = _liveHex.IsConnected;
            if (_liveHex.Session is { } s)
            {
                SessionInfo = $"Game: {s.ProfileLabel}   Title: {s.TitleId}   sys-botbase: {s.BotbaseVersion}";
                Status = $"Connected to {s.ProfileLabel}.";
            }
            else
            {
                Status = "Connected.";
            }
        }
        catch (LiveHexConnectionException ex)
        {
            IsConnected = false;
            SessionInfo = string.Empty;
            Status = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            SessionInfo = string.Empty;
            Status = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        IsBusy = true;
        try
        {
            await _liveHex.DisconnectAsync();
        }
        finally
        {
            IsConnected = false;
            SessionInfo = string.Empty;
            Status = "Disconnected.";
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanTransfer))]
    private async Task ReadBoxAsync()
    {
        var box = _getCurrentBox();
        IsBusy = true;
        Status = $"Reading Box {box + 1} from the console…";
        try
        {
            await _liveHex.ReadBoxAsync(_sav, box);
            _onBoxUpdated();
            Status = $"Read Box {box + 1} from the console.";
        }
        catch (LiveHexConnectionException ex)
        {
            Status = $"Read failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            Status = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanTransfer))]
    private async Task WriteBoxAsync()
    {
        var box = _getCurrentBox();

        // HARD REQUIREMENT: never write to the console without explicit user confirmation.
        var confirmed = await _dialogs.ShowConfirmationAsync(
            "Write box to console",
            $"This will OVERWRITE Box {box + 1} on the connected console ({GameName}) with the " +
            $"contents currently shown in PKHeX.\n\nThis cannot be undone on the console. Continue?",
            confirmText: "Write to console",
            cancelText: "Cancel");

        if (!confirmed)
        {
            Status = "Write cancelled.";
            return;
        }

        IsBusy = true;
        Status = $"Writing Box {box + 1} to the console…";
        try
        {
            await _liveHex.WriteBoxAsync(_sav, box);
            Status = $"Wrote Box {box + 1} to the console.";
        }
        catch (LiveHexConnectionException ex)
        {
            Status = $"Write failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            Status = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
