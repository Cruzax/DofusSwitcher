using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DofusSwitcher.Core;
using DofusSwitcher.Services;
using DofusSwitcher.UI;

namespace DofusSwitcher;

/// <summary>
/// Démarrage : instance unique (un second lancement avec "--settings" ouvre les réglages de la première), journal
/// des erreurs, contrôleur, barre flottante, étiquettes de mosaïque et icône près de l'horloge.
/// </summary>
public partial class App : Application
{
    private const string OpenSettingsEventName = "DofusSwitcher.OpenSettings";

    private readonly ErrorLog _errors = new(ErrorLog.DefaultPath);
    private Mutex? _singleInstance;
    private bool _ownsMutex;
    private EventWaitHandle? _openSettingsSignal;
    private RegisteredWaitHandle? _openSettingsWait;
    private bool _errorShown;
    private SwitcherController? _controller;
    private FloatingBar? _bar;
    private MosaicOverlay? _overlay;
    private TrayIcon? _tray;
    private SettingsWindow? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        WatchUnhandledErrors();

        _singleInstance = new Mutex(true, "DofusSwitcher.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            // "--settings" ouvre les réglages de l'instance déjà lancée.
            if (e.Args.Contains("--settings") && EventWaitHandle.TryOpenExisting(OpenSettingsEventName, out var signal))
            {
                using (signal) signal.Set();
            }
            else
            {
                MessageBox.Show("Dofus Switcher est déjà lancé (icône près de l'horloge).", "Dofus Switcher");
            }
            Shutdown();
            return;
        }
        ListenForOpenSettings();

        _controller = new SwitcherController(new ConfigStore(ConfigStore.DefaultPath), _errors);
        _controller.Start();

        _overlay = new MosaicOverlay(_controller);
        _bar = new FloatingBar(_controller);
        _bar.SettingsRequested += OpenSettings;
        _bar.QuitRequested += Quit;
        _bar.Show();

        _tray = new TrayIcon(_controller, OpenSettings, () => _bar?.ToggleHiddenByUser(), Quit);

        if (_controller.HotkeyFailures.Count > 0) ShowHotkeyWarning(_controller);
        if (_controller.Config.Characters.Count == 0 || e.Args.Contains("--settings")) OpenSettings();
    }

    /// <summary>Une erreur imprévue est notée dans errors.log ; sur le thread de l'interface, l'appli continue.</summary>
    private void WatchUnhandledErrors()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            _errors.Write("Erreur dans l'interface", e.Exception);
            e.Handled = true;
            // Une seule fois par lancement : une erreur répétée (minuteur…) ne doit pas empiler les fenêtres.
            if (_errorShown) return;
            _errorShown = true;
            MessageBox.Show("Une erreur inattendue s'est produite. Dofus Switcher continue de tourner.\n\nDétails : "
                + _errors.FilePath, "Dofus Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            // Avant tout : ne jamais laisser l'utilisateur sans barre des tâches.
            TaskbarHider.Show();
            if (e.ExceptionObject is Exception ex) _errors.Write("Erreur fatale", ex);
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => TaskbarHider.Show();
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _errors.Write("Erreur dans une tâche de fond", e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>Attend le signal d'un second lancement avec "--settings".</summary>
    private void ListenForOpenSettings()
    {
        _openSettingsSignal = new EventWaitHandle(false, EventResetMode.AutoReset, OpenSettingsEventName);
        _openSettingsWait = ThreadPool.RegisterWaitForSingleObject(_openSettingsSignal,
            (_, _) => Dispatcher.BeginInvoke(new Action(OpenSettings)), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    private static void ShowHotkeyWarning(SwitcherController controller)
    {
        var lines = controller.HotkeyFailures.Select(f => $"• {f.Label} ({f.Hotkey})");
        MessageBox.Show(
            "Ces raccourcis n'ont pas pu être activés (déjà utilisés par une autre appli) :\n\n"
            + string.Join("\n", lines)
            + "\n\nChange-les dans les réglages.",
            "Dofus Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OpenSettings()
    {
        if (_controller is null) return;
        if (_settings is not null)
        {
            _settings.Activate();
            return;
        }
        _settings = new SettingsWindow(_controller);
        _settings.Closed += (_, _) => _settings = null;
        _settings.Show();
        _settings.Activate();
    }

    private void Quit()
    {
        _tray?.Dispose();
        _tray = null;
        _controller?.Dispose();
        _controller = null;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _controller?.Dispose();
        _openSettingsWait?.Unregister(null);
        _openSettingsSignal?.Dispose();
        if (_ownsMutex) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
