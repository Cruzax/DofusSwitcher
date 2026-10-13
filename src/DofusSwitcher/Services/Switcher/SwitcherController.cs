using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Relie la config, la détection des fenêtres, les raccourcis et la barre. Découpé par thème :
/// ce fichier (persos, config, fenêtre active), <c>.Hotkeys</c>, <c>.Mosaic</c> et <c>.Clients</c>.
/// </summary>
public sealed partial class SwitcherController : ObservableObject, IDisposable
{
    private readonly ConfigStore _store;
    private readonly ErrorLog _errors;
    private readonly WindowScanner _scanner;
    private readonly ForegroundWatcher _foreground = new();
    private readonly FlashWatcher _flashes = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;

    private Dictionary<string, IntPtr> _windows = new(StringComparer.OrdinalIgnoreCase);
    private string? _lastActive;
    /// <summary>Un client Dofus ou l'appli (barre, réglages) est au premier plan : la barre reste affichée.</summary>
    private bool _isGameInFront = true;
    /// <summary>Un client Dofus lui-même est au premier plan : les étiquettes de mosaïque s'affichent.</summary>
    private bool _isClientInFront = true;
    private BarLayout _layout;

    public SwitcherController(ConfigStore store, ErrorLog errors)
    {
        _store = store;
        _errors = errors;
        Config = store.Load();
        _scanner = new WindowScanner(Config.ProcessNames);
        _layout = BarLayout.For(Config);
        Appearances = new ClassAppearanceProvider(ClassImageProvider.DefaultFolder);
    }

    public AppConfig Config { get; private set; }
    public ClassAppearanceProvider Appearances { get; }
    public ObservableCollection<CharacterSlot> Slots { get; } = new();
    public IReadOnlyList<GameWindow> LastScan { get; private set; } = Array.Empty<GameWindow>();

    /// <summary>Orientation et tailles de la barre flottante.</summary>
    public BarLayout Layout
    {
        get => _layout;
        private set => Set(ref _layout, value);
    }

    /// <summary>Faux quand le masquage auto est actif et qu'aucun client Dofus n'est au premier plan.</summary>
    public bool IsBarWanted => !Config.AutoHideBar || _isGameInFront;

    public void Start()
    {
        RebuildSlots();
        RegisterHotkeys();
        Refresh();
        _foreground.ForegroundChanged += OnForegroundChanged;
        _flashes.WindowFlashed += OnWindowFlashed;
        Appearances.ImagesChanged += UpdateClasses;
        _animator.Finished += UpdateMosaicLabels;
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    public void Refresh()
    {
        LastScan = _scanner.Scan();
        var windows = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in LastScan) windows.TryAdd(window.CharacterName, window.Handle);
        _windows = windows;

        RememberClasses();
        foreach (var slot in Slots)
        {
            slot.IsOnline = _windows.ContainsKey(slot.Name);
            if (!slot.IsOnline) slot.IsAlerting = false;
        }
        UpdateClasses();
        EndMosaicIfClientsGone();
        OnForegroundChanged(NativeMethods.GetForegroundWindow());
    }

    // ----- Changer de perso -----

    /// <summary>Affiche la fenêtre du perso. Renvoie false si son client est fermé.</summary>
    public bool ActivateCharacter(string name)
    {
        if (!_windows.ContainsKey(name)) Refresh();
        if (!_windows.TryGetValue(name, out var hwnd)) return false;

        if (!WindowActivator.Activate(hwnd))
        {
            // La fenêtre a peut-être été fermée/relancée depuis le dernier scan.
            Refresh();
            if (!_windows.TryGetValue(name, out hwnd) || !WindowActivator.Activate(hwnd)) return false;
        }
        _lastActive = name;
        OnForegroundChanged(hwnd);
        return true;
    }

    public void Step(int direction)
    {
        var current = CharacterAt(NativeMethods.GetForegroundWindow()) ?? _lastActive;
        var order = Config.Characters.Select(c => c.Name).ToList();
        // Un client fermé depuis le dernier scan fait échouer l'affichage : le rescan l'a retiré, on passe au suivant.
        for (int attempt = 0; attempt < order.Count; attempt++)
        {
            var target = CharacterCycle.Step(order, _windows.ContainsKey, current, direction);
            if (target is null || ActivateCharacter(target)) return;
            current = target;
        }
    }

    /// <summary>Déplace un perso dans la barre (glisser-déposer) et enregistre le nouvel ordre.</summary>
    public void MoveCharacter(string name, int newIndex)
    {
        int oldIndex = Slots.ToList().FindIndex(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (oldIndex < 0 || !Config.MoveCharacter(name, newIndex)) return;
        Slots.Move(oldIndex, Config.Characters.IndexOf(Config.FindCharacter(name)!));
        SaveConfig();
    }

    // ----- Config et barre -----

    /// <summary>Aperçu de la barre pendant les réglages, sans rien enregistrer.</summary>
    public void PreviewLayout(BarOrientation orientation, BarSize size) => Layout = BarLayout.For(orientation, size);

    /// <summary>Annule l'aperçu : revient à la mise en page enregistrée.</summary>
    public void RestoreLayout() => Layout = BarLayout.For(Config);

    public void SaveConfig()
    {
        try
        {
            _store.Save(Config);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fichier bloqué un instant (antivirus, synchro…) : la config reste en mémoire, nouvel essai au prochain enregistrement.
            _errors.Write("Enregistrement de la config", ex);
        }
    }

    public void ApplyConfig(AppConfig config)
    {
        Config = config;
        SaveConfig();
        _scanner.SetProcessNames(config.ProcessNames);
        Layout = BarLayout.For(config);
        RebuildSlots();
        _hotkeysSuspended = false;
        RegisterHotkeys();
        Refresh();
        OnPropertyChanged(nameof(IsBarWanted));
        // Le réglage "perso actif en grand" a pu changer : la mosaïque en cours suit.
        if (IsMosaic) _animator.AnimateTo(MosaicTargets());
    }

    public void SaveBarPosition(double left, double top)
    {
        Config.BarLeft = left;
        Config.BarTop = top;
        SaveConfig();
    }

    // ----- Persos de la barre -----

    /// <summary>Retient la classe vue dans le titre, pour l'afficher même quand le client est fermé.</summary>
    private void RememberClasses()
    {
        bool changed = false;
        foreach (var window in LastScan)
        {
            var character = Config.FindCharacter(window.CharacterName);
            if (window.ClassName is null || character is null || character.ClassName == window.ClassName) continue;
            character.ClassName = window.ClassName;
            changed = true;
        }
        if (changed) SaveConfig();
    }

    private void UpdateClasses()
    {
        foreach (var slot in Slots)
        {
            var className = Config.FindCharacter(slot.Name)?.ClassName;
            slot.SetClass(className, Appearances.Get(className, slot.Name));
        }
    }

    private void RebuildSlots()
    {
        Slots.Clear();
        foreach (var character in Config.Characters)
        {
            Slots.Add(new CharacterSlot(character.Name, character.Hotkey, character.Initiative,
                Appearances.Get(character.ClassName, character.Name)));
        }
    }

    // ----- Fenêtre active et alertes -----

    private void OnForegroundChanged(IntPtr foregroundWindow)
    {
        var active = CharacterAt(foregroundWindow);
        if (active is not null) _lastActive = active;
        foreach (var slot in Slots)
        {
            slot.IsActive = string.Equals(slot.Name, active, StringComparison.OrdinalIgnoreCase);
            if (slot.IsActive) slot.IsAlerting = false;
        }

        // Pendant un changement de fenêtre, Windows passe parfois brièvement par "aucune fenêtre" : on l'ignore.
        if (foregroundWindow == IntPtr.Zero) return;
        FollowFocus(foregroundWindow);
        bool clientInFront = _scanner.IsGameWindow(foregroundWindow);
        bool gameInFront = clientInFront || IsOwnWindow(foregroundWindow);
        if (clientInFront == _isClientInFront && gameInFront == _isGameInFront) return;
        _isClientInFront = clientInFront;
        _isGameInFront = gameInFront;
        OnPropertyChanged(nameof(IsBarWanted));
        UpdateTaskbar();
        UpdateMosaicLabels();
    }

    private void OnWindowFlashed(IntPtr hwnd)
    {
        if (hwnd == NativeMethods.GetForegroundWindow()) return;
        var name = CharacterAt(hwnd);
        var slot = Slots.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (slot is not null) slot.IsAlerting = true;
    }

    private bool IsOwnWindow(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == _ownProcessId;
    }

    private string? CharacterAt(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        foreach (var (name, handle) in _windows)
        {
            if (handle == hwnd) return name;
        }
        return null;
    }

    public void Dispose()
    {
        TaskbarHider.Show();
        _animator.Stop();
        _timer.Stop();
        _foreground.Dispose();
        _flashes.Dispose();
        _hotkeys.Dispose();
    }
}
