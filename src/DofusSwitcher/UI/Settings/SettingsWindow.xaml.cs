using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DofusSwitcher.Core;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>
/// Fenêtre des réglages. Ce fichier : ouverture, options de la barre, enregistrement. Les autres parties :
/// <c>.Team</c> (persos et initiative), <c>.Hotkeys</c> (touches), <c>.MosaicPreview</c> (vignettes).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SwitcherController _controller;
    private bool _barOptionsReady;
    private bool _saved;

    public ObservableCollection<EditableCharacter> Characters { get; } = new();
    public ObservableCollection<DetectedCharacter> Detected { get; } = new();
    public IReadOnlyList<HotkeyField> NavigationFields { get; }


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

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    private void HideStatus() => StatusText.Visibility = Visibility.Collapsed;

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
