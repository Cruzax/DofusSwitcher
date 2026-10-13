using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DofusSwitcher.Core;
using DofusSwitcher.Services;
using DofusSwitcher.UI;
using Forms = System.Windows.Forms;

namespace DofusSwitcher;

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
    private Forms.NotifyIcon? _tray;
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

        _tray = CreateTrayIcon();

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

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        var pauseItem = menu.Items.Add("", null, (_, _) => _controller?.TogglePause());
        menu.Opening += (_, _) => pauseItem.Text = _controller?.IsPaused == true ? "Reprendre" : "Mettre en pause";
        menu.Items.Add("Réglages", null, (_, _) => OpenSettings());
        menu.Items.Add("Afficher / masquer la barre", null, (_, _) => _bar?.ToggleHiddenByUser());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => Quit());

        var tray = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "Dofus Switcher",
            ContextMenuStrip = menu,
            Visible = true,
        };
        tray.DoubleClick += (_, _) => OpenSettings();
        return tray;
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var resource = GetResourceStream(new System.Uri("pack://application:,,,/Assets/app.ico"));
        if (resource is null) return System.Drawing.SystemIcons.Application;
        using var stream = resource.Stream;
        // Taille adaptée à la zone de notification (16 px à 100 %, plus en haute résolution).
        return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
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
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
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
