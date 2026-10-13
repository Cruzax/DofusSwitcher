using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using DofusSwitcher.Core;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>
/// Glisser-déposer "à la iPhone" : l'icône prise grossit et suit la souris le long de la barre ; les autres glissent
/// pour lui laisser la place ; au lâcher elle se cale dans le trou et le nouvel ordre est enregistré.
/// </summary>
public partial class FloatingBar
{
    private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(160);
    private static readonly IEasingFunction Ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
    private const double LiftScale = 1.12;
    private const double SlotSpacing = 6; // marge de 3 px de chaque côté d'une icône

    private Point _pressPoint;
    private CharacterSlot? _pressedSlot;
    private bool _suppressNextClick;
    private ReorderDrag? _drag;

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
}
