using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PointUp.Wpf.ViewModels;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace PointUp.Wpf.Views;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private IntPtr _hwnd;
    private readonly OverlayViewModel _viewModel;

    private class StrokeVisual
    {
        public List<Line> Segments { get; } = [];
        public long CreatedAtMs { get; set; }
    }

    private const double CursorCircleSize = 20;

    private readonly List<StrokeVisual> _strokes = [];
    private StrokeVisual? _currentStroke;
    private Point _lastPoint;
    private long _lastTimestampMs;
    private double _currentThickness;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly DispatcherTimer _fadeTimer;
    private Brush _lineBrush = Brushes.OrangeRed;
    private Ellipse? _cursorCircle;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        _fadeTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _fadeTimer.Tick += OnFadeTick;
        _fadeTimer.Start();

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ClearRequested += (_, _) => ClearCanvas();
        viewModel.SettingsReloaded += (_, _) => ApplySettings();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;

        int style = GetWindowLong(_hwnd, GWL_EXSTYLE);
        style |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        SetWindowLong(_hwnd, GWL_EXSTYLE, style);

        SetClickThrough(!_viewModel.IsPointingEnabled);
        ApplySettings();
        InitCursorCircle();
        UpdatePointingModeCursor(_viewModel.IsPointingEnabled);
    }

    private void SetClickThrough(bool enabled)
    {
        if (_hwnd == IntPtr.Zero) return;
        int style = GetWindowLong(_hwnd, GWL_EXSTYLE);
        if (enabled)
            style |= WS_EX_TRANSPARENT;
        else
            style &= ~WS_EX_TRANSPARENT;
        SetWindowLong(_hwnd, GWL_EXSTYLE, style);
    }

    private void ApplySettings()
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_viewModel.Settings.LineColor);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            _lineBrush = brush;
        }
        catch
        {
            _lineBrush = Brushes.OrangeRed;
        }

        if (_cursorCircle != null)
            _cursorCircle.Stroke = _lineBrush;
    }

    private void InitCursorCircle()
    {
        _cursorCircle = new Ellipse
        {
            Width = CursorCircleSize,
            Height = CursorCircleSize,
            Stroke = _lineBrush,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        System.Windows.Controls.Panel.SetZIndex(_cursorCircle, int.MaxValue);
        Canvas.SetLeft(_cursorCircle, -CursorCircleSize);
        Canvas.SetTop(_cursorCircle, -CursorCircleSize);
        DrawingCanvas.Children.Add(_cursorCircle);
    }

    private void UpdatePointingModeCursor(bool isOn)
    {
        Cursor = isOn ? Cursors.None : Cursors.Arrow;
        if (_cursorCircle != null)
            _cursorCircle.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.IsPointingEnabled)) return;

        bool isOn = _viewModel.IsPointingEnabled;
        SetClickThrough(!isOn);
        UpdatePointingModeCursor(isOn);

        if (!isOn && _currentStroke != null)
            FinalizeCurrentStroke();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!_viewModel.IsPointingEnabled) return;

        _lastPoint = e.GetPosition(DrawingCanvas);
        _lastTimestampMs = _stopwatch.ElapsedMilliseconds;
        _currentThickness = _viewModel.Settings.Thickness;
        _currentStroke = new StrokeVisual();

        Mouse.Capture(this);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        if (_viewModel.IsPointingEnabled && _cursorCircle != null)
        {
            Canvas.SetLeft(_cursorCircle, pos.X - CursorCircleSize / 2);
            Canvas.SetTop(_cursorCircle, pos.Y - CursorCircleSize / 2);
        }

        if (_currentStroke == null || !_viewModel.IsPointingEnabled) return;

        double dist = (pos - _lastPoint).Length;
        if (dist < 2.0) return;

        long now = _stopwatch.ElapsedMilliseconds;
        long dt = now - _lastTimestampMs;
        if (dt > 0)
        {
            double speed = dist / dt;
            _currentThickness = _viewModel.CalculateThickness(speed, _currentThickness);
        }

        var seg = new Line
        {
            X1 = _lastPoint.X, Y1 = _lastPoint.Y,
            X2 = pos.X, Y2 = pos.Y,
            Stroke = _lineBrush,
            StrokeThickness = _currentThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        DrawingCanvas.Children.Add(seg);
        _currentStroke.Segments.Add(seg);

        _lastPoint = pos;
        _lastTimestampMs = now;
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_currentStroke == null) return;
        FinalizeCurrentStroke();
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void FinalizeCurrentStroke()
    {
        if (_currentStroke == null) return;
        _currentStroke.CreatedAtMs = _stopwatch.ElapsedMilliseconds;
        _strokes.Add(_currentStroke);
        _currentStroke = null;
    }

    private void OnFadeTick(object? sender, EventArgs e)
    {
        if (_strokes.Count == 0) return;

        long now = _stopwatch.ElapsedMilliseconds;
        List<StrokeVisual> toRemove = [];

        foreach (var stroke in _strokes)
        {
            long age = now - stroke.CreatedAtMs;
            var (opacity, expired) = _viewModel.CalculateLifetime(age);

            if (expired)
            {
                toRemove.Add(stroke);
            }
            else
            {
                foreach (var seg in stroke.Segments)
                    seg.Opacity = opacity;
            }
        }

        foreach (var stroke in toRemove)
        {
            foreach (var seg in stroke.Segments)
                DrawingCanvas.Children.Remove(seg);
            _strokes.Remove(stroke);
        }
    }

    private void ClearCanvas()
    {
        _currentStroke = null;
        Mouse.Capture(null);
        DrawingCanvas.Children.Clear();
        _strokes.Clear();
        if (_cursorCircle != null)
            DrawingCanvas.Children.Add(_cursorCircle);
    }
}
