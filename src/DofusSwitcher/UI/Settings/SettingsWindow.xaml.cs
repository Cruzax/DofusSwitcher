using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusSwitcher.Core;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

public partial class SettingsWindow : Window
{
    private static readonly string[] DefaultCharacterHotkeys = { "F1", "F2", "F3", "F4" };

    private readonly SwitcherController _controller;
    private bool _barOptionsReady;
    private bool _saved;

    public ObservableCollection<EditableCharacter> Characters { get; } = new();
    public ObservableCollection<DetectedCharacter> Detected { get; } = new();
    public IReadOnlyList<HotkeyField> NavigationFields { get; }

    /// <summary>Aperçus des deux mosaïques, calculés comme la vraie (le 1er de l'équipe sert de perso actif).</summary>
    public ObservableCollection<PreviewTile> GridPreview { get; } = new();
    public ObservableCollection<PreviewTile> FocusPreview { get; } = new();

    /// <summary>Taille des vignettes (forme d'un écran 16:9), et sous quelle largeur une case passe en compact.</summary>
    public const double PreviewWidth = 248;
    public const double PreviewHeight = PreviewWidth * 9 / 16;
    private const double CompactBelow = 100;

    public SettingsWindow(SwitcherController controller)
    {
        _controller = controller;
        var config = controller.Config;

        foreach (var character in config.Characters)
        {
            Characters.Add(CreateCharacter(character.Name, character.ClassName, character.Hotkey, character.Initiative));
        }
        NavigationFields = new[]
        {
            new HotkeyField(HotkeyOwners.Next, "Perso suivant", config.NextHotkey),
            new HotkeyField(HotkeyOwners.Previous, "Perso précédent", config.PreviousHotkey),
            new HotkeyField(HotkeyOwners.Pause, "Pause / reprise", config.PauseHotkey),
            new HotkeyField(HotkeyOwners.Mosaic, "Mosaïque / plein écran", config.MosaicHotkey),
        };

        InitializeComponent();
        DataContext = this;

        // Titre, pied de fenêtre et marges ≈ 220 px : le reste défile sur les petits écrans.
        ContentScroll.MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 220);

        AutoHideSwitch.IsChecked = config.AutoHideBar;
        (config.MosaicFocusActive ? MosaicFocusOption : MosaicGridOption).IsChecked = true;
        (config.BarOrientation == BarOrientation.Vertical ? VerticalOption : HorizontalOption).IsChecked = true;
        (config.BarSize switch { BarSize.Small => SmallOption, BarSize.Large => LargeOption, _ => NormalOption }).IsChecked = true;
        _barOptionsReady = true;

        // Fermer sans enregistrer annule l'aperçu de la barre.
        Closed += (_, _) =>
        {
            if (!_saved) controller.RestoreLayout();
        };

        // Sinon les raccourcis globaux interceptent les touches avant qu'on puisse les saisir.
        controller.SuspendHotkeys();
        Closed += (_, _) => controller.ResumeHotkeys();

        Characters.CollectionChanged += (_, _) =>
        {
            UpdateCharacterCount();
            UpdateMosaicPreview();
        };
        UpdateCharacterCount();
        UpdateMosaicPreview();
    }

    private IEnumerable<HotkeyField> AllHotkeyFields => Characters.Concat(NavigationFields);

    private EditableCharacter CreateCharacter(string name, string? className, string? hotkey, int? initiative = null) =>
        new(name, className, hotkey, initiative, _controller.Appearances.Get(className, name));

    private void UpdateCharacterCount()
    {
        CountText.Text = $"{Characters.Count} sur {AppConfig.MaxCharacters}";
        bool canAdd = Characters.Count < AppConfig.MaxCharacters;
        AddSlot.Visibility = canAdd && AddPanel.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        if (!canAdd) AddPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>Les vignettes de la mosaïque montrent l'équipe telle qu'elle est (ordre, ajouts, retraits).</summary>
    private void UpdateMosaicPreview()
    {
        // Même calcul que la vraie mosaïque, sur un écran 1920×1080, puis réduit à la vignette.
        var screen = new TileRect(0, 0, 1920, 1080);
        Fill(GridPreview, MosaicLayout.Tiles(Characters.Count, screen));
        Fill(FocusPreview, MosaicLayout.Focus(Characters.Count, screen));

        void Fill(ObservableCollection<PreviewTile> preview, IReadOnlyList<TileRect> tiles)
        {
            double scale = PreviewWidth / screen.Width;
            preview.Clear();
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                double width = t.Width * scale;
                preview.Add(new PreviewTile(Characters[i], t.X * scale, t.Y * scale, width, t.Height * scale,
                    Compact: width < CompactBelow, IsActive: i == 0));
            }
        }
    }

    // ----- Ajout de persos -----

    private void AddSlot_Click(object sender, RoutedEventArgs e)
    {
        AddPanel.Visibility = Visibility.Visible;
        AddSlot.Visibility = Visibility.Collapsed;
        RefreshDetected();
    }

    private void RefreshDetected()
    {
        _controller.Refresh();
        Detected.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in _controller.LastScan)
        {
            if (Contains(window.CharacterName) || !seen.Add(window.CharacterName)) continue;
            Detected.Add(new DetectedCharacter(window.CharacterName, window.ClassName,
                _controller.Appearances.Get(window.ClassName, window.CharacterName)));
        }
        NoDetectedHint.Visibility = Detected.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool Contains(string name) =>
        Characters.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private void AddCharacter(string name, string? className)
    {
        name = name.Trim();
        if (name.Length == 0 || Contains(name) || Characters.Count >= AppConfig.MaxCharacters) return;

        var used = AllHotkeyFields.Select(f => f.Hotkey).ToList();
        var hotkey = DefaultCharacterHotkeys.FirstOrDefault(k => !used.Contains(k, StringComparer.OrdinalIgnoreCase));
        Characters.Add(CreateCharacter(name, className, hotkey));

        AddPanel.Visibility = Visibility.Collapsed;
        UpdateCharacterCount();
        Validate();
    }

    private void AddDetected_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DetectedCharacter detected }) AddCharacter(detected.Name, detected.ClassName);
    }

    private void AddManual_Click(object sender, RoutedEventArgs e) => AddManual();

    private void ManualName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        AddManual();
        e.Handled = true;
    }

    private void AddManual()
    {
        var name = ManualName.Text;
        ManualName.Text = "";
        // Un perso connecté garde sa classe même s'il est ajouté à la main.
        var className = _controller.LastScan
            .FirstOrDefault(w => string.Equals(w.CharacterName, name.Trim(), StringComparison.OrdinalIgnoreCase))?.ClassName;
        AddCharacter(name, className);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDetected();

    // ----- Liste des persos -----

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(sender, -1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(sender, 1);

    private void Move(object sender, int offset)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        int index = Characters.IndexOf(character);
        int target = index + offset;
        if (index >= 0 && target >= 0 && target < Characters.Count) Characters.Move(index, target);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        Characters.Remove(character);
        Validate();
        if (AddPanel.Visibility == Visibility.Visible) RefreshDetected();
    }

    // ----- Raccourcis -----

    /// <summary>Perso sans touche : on l'affiche en cliquant sur la barre ou avec Suivant / Précédent.</summary>
    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        character.Hotkey = null;
        Validate();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.Tab || Hotkey.IsModifierKey(key)) return;

        box.Text = key is Key.Delete or Key.Back or Key.Escape ? "" : new Hotkey(Keyboard.Modifiers, key).ToString();
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Keyboard.ClearFocus();
        Validate();
    }

    /// <summary>Signale en rouge les touches utilisées par plusieurs actions. Renvoie false s'il y en a.</summary>
    private bool Validate()
    {
        var duplicates = HotkeyConflicts.FindDuplicates(AllHotkeyFields.Select(f => (f.Owner, f.Hotkey)));
        foreach (var field in AllHotkeyFields) field.HasError = duplicates.Contains(field.Owner);

        if (duplicates.Count > 0)
        {
            ShowStatus("Une même touche est utilisée pour plusieurs actions.");
            return false;
        }
        if (Characters.Any(c => c.HasInitiativeError))
        {
            ShowStatus("L'initiative doit être un nombre (ex. 1250), ou rester vide.");
            return false;
        }
        HideStatus();
        return true;
    }

    // ----- Initiative -----

    /// <summary>Range l'équipe dans l'ordre des tours : la plus haute initiative d'abord, les inconnues à la fin.</summary>
    private void SortByInitiative_Click(object sender, RoutedEventArgs e)
    {
        var sorted = InitiativeOrder.Sort(Characters, c => c.Initiative);
        for (int target = 0; target < sorted.Count; target++)
        {
            int current = Characters.IndexOf(sorted[target]);
            if (current != target) Characters.Move(current, target);
        }
    }

    private void Initiative_LostFocus(object sender, RoutedEventArgs e) => Validate();

    // ----- Barre -----

    private BarOrientation SelectedOrientation =>
        VerticalOption.IsChecked == true ? BarOrientation.Vertical : BarOrientation.Horizontal;

    private BarSize SelectedSize =>
        SmallOption.IsChecked == true ? BarSize.Small : LargeOption.IsChecked == true ? BarSize.Large : BarSize.Normal;

    /// <summary>Aperçu immédiat sur la barre ; rien n'est enregistré avant "Enregistrer".</summary>
    private void BarOption_Checked(object sender, RoutedEventArgs e)
    {
        if (_barOptionsReady) _controller.PreviewLayout(SelectedOrientation, SelectedSize);
    }

    // ----- Enregistrer / fermer -----

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        var current = _controller.Config;
        _controller.ApplyConfig(new AppConfig
        {
            Characters = Characters
                .Select(c => new CharacterConfig { Name = c.Name, Hotkey = NullIfEmpty(c.Hotkey), ClassName = c.ClassName, Initiative = c.Initiative })
                .ToList(),
            NextHotkey = NullIfEmpty(NavigationFields[0].Hotkey),
            PreviousHotkey = NullIfEmpty(NavigationFields[1].Hotkey),
            PauseHotkey = NullIfEmpty(NavigationFields[2].Hotkey),
            MosaicHotkey = NullIfEmpty(NavigationFields[3].Hotkey),
            ProcessNames = current.ProcessNames,
            BarLeft = current.BarLeft,
            BarTop = current.BarTop,
            AutoHideBar = AutoHideSwitch.IsChecked == true,
            MosaicFocusActive = MosaicFocusOption.IsChecked == true,
            BarOrientation = SelectedOrientation,
            BarSize = SelectedSize,
        });
        _saved = true;

        var failures = _controller.HotkeyFailures;
        if (failures.Count == 0)
        {
            Close();
            return;
        }

        var failedOwners = failures.Select(f => f.Owner).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var field in AllHotkeyFields) field.HasError = failedOwners.Contains(field.Owner);
        ShowStatus("Ces touches sont déjà prises par une autre appli : "
            + string.Join(", ", failures.Select(f => f.Hotkey)) + ". Choisis-en d'autres.");
        // On suspend à nouveau pour pouvoir les corriger dans cette fenêtre.
        _controller.SuspendHotkeys();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void OpenImagesFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = _controller.Appearances.ImagesFolder;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    private void HideStatus() => StatusText.Visibility = Visibility.Collapsed;

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
