using System.Diagnostics;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PKHeX.Application.Abstractions;
using PKHeX.Presentation.Localization;

namespace PKHeX.Avalonia.Services;

/// <summary>
/// Installs process-wide last-chance handlers so an unhandled exception on the UI thread, a
/// thread-pool task, or any other thread is logged (and, where the UI is still alive, surfaced
/// via an error dialog) instead of silently crashing the whole process.
/// </summary>
public static class GlobalExceptionHandler
{
    public static void Install(IServiceProvider services)
    {
        // UI-thread exceptions (e.g. thrown from a binding, command, or event handler running on
        // the dispatcher): log, mark handled so Avalonia doesn't tear down the process, and tell
        // the user something went wrong.
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Trace.TraceError($"Unhandled UI-thread exception: {e.Exception}");
            e.Handled = true;
            ShowErrorDialog(services, e.Exception);
        };

        // Fire-and-forget Tasks whose exception was never awaited/observed: log and mark
        // observed so the finalizer thread doesn't escalate it into a process crash.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Trace.TraceError($"Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };

        // Last-chance handler for exceptions on any other thread. By the time this fires the
        // runtime is already terminating the process for non-UI threads, so this is log-only.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Trace.TraceError($"Unhandled AppDomain exception (terminating={e.IsTerminating}): {e.ExceptionObject}");
        };
    }

    private static void ShowErrorDialog(IServiceProvider services, Exception ex)
    {
        var dialogService = services.GetRequiredService<IDialogService>();
        _ = dialogService.ShowErrorAsync(
            LocalizedStrings.Instance["App_UnexpectedErrorTitle"],
            LocalizedStrings.Instance.Format("App_UnexpectedErrorMessage", ex.Message));
    }
}
