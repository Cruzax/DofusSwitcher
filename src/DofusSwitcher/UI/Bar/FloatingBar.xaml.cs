using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using DofusSwitcher.Core;
using DofusSwitcher.Services;
using DofusSwitcher.Win32;

namespace DofusSwitcher.UI;

public partial class FloatingBar : Window
{
    private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(160);
    private static readonly IEasingFunction Ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
    private const double LiftScale = 1.12;
    private const double SlotSpacing = 6; // marge de 3 px de chaque côté d'une icône

    private readonly SwitcherController _controller;

    private bool _hiddenByUser;
    private Point _pressPoint;
    private CharacterSlot? _pressedSlot;
    private bool _suppressNextClick;
    private ReorderDrag? _drag;

    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    public FloatingBar(SwitcherController controller)
    {
        InitializeComponent();
        _controller = controller;
        DataContext = controller;
        controller.Slots.CollectionChanged += (_, _) => UpdateEmptyHint();
        controller.PropertyChanged += OnControllerPropertyChanged;
        UpdateEmptyHint();
        Loaded += (_, _) => PlaceOnScreen();
        // Changer de taille ou d'orientation peut faire déborder la barre près d'un bord.
        // Recadrage une fois la mise en page terminée, pas sur une taille intermédiaire.
        SizeChanged += (_, _) => Dispatcher.BeginInvoke(KeepOnScreen, System.Windows.Threading.DispatcherPriority.ContextIdle);
        IsVisibleChanged += (_, _) => Dispatcher.BeginInvoke(KeepOnScreen, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    /// <summary>Afficher / masquer depuis l'icône près de l'horloge (prioritaire sur le masquage auto).</summary>
    public void ToggleHiddenByUser()
    {
        _hiddenByUser = !_hiddenByUser;
        UpdateVisibility();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Ne vole pas le focus au jeu et n'apparaît pas dans Alt+Tab.
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            style | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
    }

    private void OnControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SwitcherController.IsBarWanted)) UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool visible = !_hiddenByUser && _controller.IsBarWanted;
        // Jamais masquée en plein glisser-déposer.
        if (!visible && _drag is not null) return;
        Visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    private void PlaceOnScreen()
    {
        var config = _controller.Config;
        if (config.BarLeft is double left && config.BarTop is double top && IsOnScreen(left, top))
        {
            Left = left;
            Top = top;
        }
        else
        {
            Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2;
            Top = 8;
        }
        KeepOnScreen();
        UpdateVisibility();
    }

    private static Rect VirtualScreen => new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

    private static bool IsOnScreen(double left, double top) => VirtualScreen.Contains(new Point(left + 10, top + 10));

    /// <summary>Ramène la barre entièrement à l'écran (et enregistre la position si elle a bougé).</summary>
    private void KeepOnScreen()
    {
        // Masquée, la fenêtre peut avoir une taille nulle ou provisoire : on ne recadre que visible.
        if (!IsLoaded || !IsVisible || ActualWidth <= 0 || double.IsNaN(Left) || double.IsNaN(Top)) return;
        var screen = VirtualScreen;
        // Pendant la mise en page, WPF donne parfois une taille provisoire plus grande que l'écran : on l'ignore.
        if (ActualWidth >= screen.Width || ActualHeight >= screen.Height) return;
        double left = Math.Max(screen.Left, Math.Min(Left, screen.Right - ActualWidth));
        double top = Math.Max(screen.Top, Math.Min(Top, screen.Bottom - ActualHeight));
        if (left == Left && top == Top) return;
        Left = left;
        Top = top;
        _controller.SaveBarPosition(Left, Top);
    }

    private void UpdateEmptyHint() =>
        EmptyHint.Visibility = _controller.Slots.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
        _controller.SaveBarPosition(Left, Top);
        KeepOnScreen();
    }

    // ----- Glisser-déposer "à la iPhone" -----
    // L'icône prise grossit et suit la souris le long de la barre ; les autres glissent pour lui
    // laisser la place ; au lâcher elle se cale dans le trou et le nouvel ordre est enregistré.

    private sealed class ReorderDrag
    {
        public required CharacterSlot Slot { get; init; }
        public required int From { get; init; }
        public required double Stride { get; init; }
        public required bool Vertical { get; init; }
        public required FrameworkElement[] Containers { get; init; }
        public required TranslateTransform[] Offsets { get; init; }
        public required ScaleTransform Lift { get; init; }
        public int Target { get; set; }
        public bool Ending { get; set; }
    }

    private void Slot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(SlotsList);
        _pressedSlot = (sender as FrameworkElement)?.Tag as CharacterSlot;
        _suppressNextClick = false;
    }

    private void Slot_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressedSlot is null || _drag is not null) return;

        var delta = e.GetPosition(SlotsList) - _pressPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        BeginReorder(_pressedSlot);
    }

    private void BeginReorder(CharacterSlot slot)
    {
        var slots = _controller.Slots;
        var containers = new FrameworkElement[slots.Count];
        var offsets = new TranslateTransform[slots.Count];
        var lift = new ScaleTransform(1, 1);
        for (int i = 0; i < slots.Count; i++)
        {
            if (SlotsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container) return;
            containers[i] = container;
            offsets[i] = new TranslateTransform();
            container.RenderTransformOrigin = new Point(0.5, 0.5);
            container.RenderTransform = slots[i] == slot
                ? new TransformGroup { Children = { lift, offsets[i] } }
                : offsets[i];
        }

        int from = slots.IndexOf(slot);
        _drag = new ReorderDrag
        {
            Slot = slot,
            From = from,
            Target = from,
            Stride = _controller.Layout.SlotSize + SlotSpacing,
            Vertical = _controller.Layout.IsVertical,
            Containers = containers,
            Offsets = offsets,
            Lift = lift,
        };

        // L'icône prise passe au-dessus des autres, grossit et projette une ombre.
        Panel.SetZIndex(containers[from], 1);
        containers[from].Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Opacity = 0.6, Color = Colors.Black };
        Animate(lift, ScaleTransform.ScaleXProperty, LiftScale);
        Animate(lift, ScaleTransform.ScaleYProperty, LiftScale);

        _suppressNextClick = true;
        SlotsList.CaptureMouse();
        Mouse.OverrideCursor = Cursors.Hand;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (_drag is null || _drag.Ending) return;

        var position = e.GetPosition(SlotsList);
        double offset = _drag.Vertical ? position.Y - _pressPoint.Y : position.X - _pressPoint.X;
        int count = _drag.Offsets.Length;
        // L'icône reste dans la barre (avec un peu de jeu aux extrémités).
        double min = -_drag.From * _drag.Stride - _drag.Stride * 0.3;
        double max = (count - 1 - _drag.From) * _drag.Stride + _drag.Stride * 0.3;
        offset = Math.Clamp(offset, min, max);

        var dragged = _drag.Offsets[_drag.From];
        if (_drag.Vertical) dragged.Y = offset;
        else dragged.X = offset;

        int target = ReorderMath.TargetIndex(_drag.From, offset, _drag.Stride, count);
        if (target == _drag.Target) return;
        _drag.Target = target;
        for (int i = 0; i < count; i++)
        {
            if (i == _drag.From) continue;
            double shift = ReorderMath.Shift(i, _drag.From, target) * _drag.Stride;
            Animate(_drag.Offsets[i], _drag.Vertical ? TranslateTransform.YProperty : TranslateTransform.XProperty, shift);
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (_drag is null) return;
        EndReorder(commit: true);
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        // Capture perdue sans lâcher (Alt+Tab…) : on remet tout en place. On ignore la capture que le bouton
        // de l'icône perd au moment où la liste la prend pour démarrer le glisser (l'événement remonte jusqu'ici).
        if (e.OriginalSource == SlotsList && _drag is { Ending: false }) EndReorder(commit: false);
    }

    /// <summary>L'icône se cale dans son emplacement final, puis l'ordre est appliqué.</summary>
    private void EndReorder(bool commit)
    {
        var drag = _drag!;
        drag.Ending = true;
        Mouse.OverrideCursor = null;
        if (SlotsList.IsMouseCaptured) SlotsList.ReleaseMouseCapture();

        int target = commit ? drag.Target : drag.From;
        var axis = drag.Vertical ? TranslateTransform.YProperty : TranslateTransform.XProperty;
        Animate(drag.Lift, ScaleTransform.ScaleXProperty, 1);
        Animate(drag.Lift, ScaleTransform.ScaleYProperty, 1);
        Animate(drag.Offsets[drag.From], axis, (target - drag.From) * drag.Stride, () =>
        {
            // Nouvel ordre et remise à zéro des décalages dans la même frame : pas de saut visible.
            if (target != drag.From) _controller.MoveCharacter(drag.Slot.Name, target);
            foreach (var container in drag.Containers)
            {
                container.RenderTransform = Transform.Identity;
                container.Effect = null;
                Panel.SetZIndex(container, 0);
            }
            _drag = null;
            _pressedSlot = null;
            UpdateVisibility();
        });
        if (!commit)
        {
            for (int i = 0; i < drag.Offsets.Length; i++)
            {
                if (i != drag.From) Animate(drag.Offsets[i], axis, 0);
            }
        }
    }

    private static void Animate(Animatable target, DependencyProperty property, double to, Action? completed = null)
    {
        var animation = new DoubleAnimation(to, SlideDuration) { EasingFunction = Ease };
        if (completed is not null) animation.Completed += (_, _) => completed();
        target.BeginAnimation(property, animation);
    }

    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            return;
        }
        if (sender is FrameworkElement { Tag: CharacterSlot slot }) _controller.ActivateCharacter(slot.Name);
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _controller.TogglePause();

    private async void Mosaic_Click(object sender, RoutedEventArgs e)
    {
        var result = await _controller.ToggleMosaicAsync();
        if (result.SkippedFullscreen == 0) return;
        MessageBox.Show(
            (result.SkippedFullscreen == 1 ? "Un client est réglé" : $"{result.SkippedFullscreen} clients sont réglés")
            + " sur « Plein écran » dans Dofus et ne peut pas être rangé.\n\n"
            + "Dans Dofus : Options → Affichage → Mode d'affichage → « Fenêtré ». Le bouton s'occupe ensuite "
            + "du plein écran (sans bordure) et de la mosaïque.",
            "Dofus Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ----- Fermer tous les clients -----
    // Un clic de travers ne doit pas couper toute la team : le 1er clic arme le bouton, le 2e (sous 3 s) ferme.

    private System.Windows.Threading.DispatcherTimer? _closeAllDisarm;

    private async void CloseAll_Click(object sender, RoutedEventArgs e)
    {
        if (CloseAllButton.Tag as string == "Armed")
        {
            DisarmCloseAll();
            await _controller.CloseAllGameWindowsAsync();
            return;
        }

        int count = _controller.CountGameWindows();
        if (count == 0)
        {
            CloseAllButton.ToolTip = "Aucun client Dofus ouvert";
        }
        else
        {
            CloseAllButton.Tag = "Armed";
            CloseAllButton.ToolTip = count == 1 ? "Clique encore pour fermer le client" : $"Clique encore pour fermer les {count} clients";
        }
        // Le minuteur désarme le bouton et remet l'info-bulle d'origine.
        _closeAllDisarm ??= CreateDisarmTimer();
        _closeAllDisarm.Stop();
        _closeAllDisarm.Start();
    }

    private System.Windows.Threading.DispatcherTimer CreateDisarmTimer()
    {
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) => DisarmCloseAll();
        return timer;
    }

    private void DisarmCloseAll()
    {
        _closeAllDisarm?.Stop();
        CloseAllButton.ClearValue(TagProperty);
        CloseAllButton.ClearValue(ToolTipProperty);
    }

    private void BarMenu_Opened(object sender, RoutedEventArgs e) =>
        PauseMenuItem.Header = _controller.IsPaused ? "Reprendre" : "Mettre en pause";

    // Pour la réafficher : icône près de l'horloge → Afficher / masquer la barre.
    private void HideBar_Click(object sender, RoutedEventArgs e) => ToggleHiddenByUser();

    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void Quit_Click(object sender, RoutedEventArgs e) => QuitRequested?.Invoke();
}
