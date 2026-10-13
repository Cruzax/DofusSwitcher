using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DofusSwitcher.Services;
using DofusSwitcher.Win32;

namespace DofusSwitcher.UI;

/// <summary>
/// Barre flottante : une icône par perso et quelques boutons. Ce fichier gère l'affichage et la place à l'écran ;
/// <c>.Reorder</c> le glisser-déposer des icônes, <c>.Buttons</c> les clics.
/// </summary>
public partial class FloatingBar : Window
{
    private readonly SwitcherController _controller;

    private bool _hiddenByUser;

    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    public FloatingBar(SwitcherController controller)
    {
        InitializeComponent();
        _controller = controller;
        DataContext = controller;
        controller.Slots.CollectionChanged += (_, _) => UpdateEmptyHint();
        controller.PropertyChanged += OnControllerPropertyChanged;
        controller.FullscreenClientsSkipped += OnFullscreenClientsSkipped;
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
}
