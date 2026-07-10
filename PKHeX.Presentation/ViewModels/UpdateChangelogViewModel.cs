using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Application.Abstractions;
using PKHeX.Application.Services;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// "What's new" dialog: renders the release notes for every version between the installed build
/// and the latest one, with a link to the release page for full details/assets.
/// </summary>
public partial class UpdateChangelogViewModel : ViewModelBase, ICloseableDialog
{
    private readonly IReadOnlyList<ReleaseInfo> _releasesNewestFirst;

    public Action? CloseRequested { get; set; }

    public IReadOnlyList<ReleaseNoteViewModel> Releases { get; }

    public string LatestVersion { get; }
    public string LatestReleaseUrl { get; }

    public UpdateChangelogViewModel(IReadOnlyList<ReleaseInfo> releasesNewestFirst)
    {
        _releasesNewestFirst = releasesNewestFirst;

        Releases = releasesNewestFirst
            .Select(r => new ReleaseNoteViewModel(r.TagName, r.Name, r.Body, r.HtmlUrl))
            .ToList();

        var latest = releasesNewestFirst.Count > 0 ? releasesNewestFirst[0] : null;
        LatestVersion = latest?.TagName ?? string.Empty;
        LatestReleaseUrl = latest?.HtmlUrl ?? string.Empty;
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        OpenUrl(LatestReleaseUrl);
    }

    [RelayCommand]
    private void Download()
    {
        DownloadLatestRelease(_releasesNewestFirst);
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// Opens the release asset matching the current OS/architecture (see <see cref="AssetSelector"/>),
    /// falling back to the release page itself when no asset matches (e.g. a release with no
    /// binaries attached, or an unrecognized platform). Shared by the changelog dialog and the
    /// status-bar notification so both "Download" actions behave identically.
    /// </summary>
    internal static void DownloadLatestRelease(IReadOnlyList<ReleaseInfo> releasesNewestFirst)
    {
        if (releasesNewestFirst.Count == 0)
            return;

        var latest = releasesNewestFirst[0];
        var asset = AssetSelector.SelectAsset(latest.Assets, CurrentPlatform.Os, CurrentPlatform.Arch);
        OpenUrl(asset?.BrowserDownloadUrl ?? latest.HtmlUrl);
    }

    internal static void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else if (OperatingSystem.IsLinux())
                Process.Start("xdg-open", url);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to open release/asset URL '{url}': {ex.Message}");
        }
    }
}

/// <summary>A single release's notes, formatted for display in the changelog dialog.</summary>
public sealed class ReleaseNoteViewModel(string tagName, string name, string body, string htmlUrl)
{
    public string TagName { get; } = tagName;
    public string Name { get; } = name;
    public string Body { get; } = body;
    public string HtmlUrl { get; } = htmlUrl;
}
