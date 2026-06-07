using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PointUp.Wpf.ViewModels;
using Color = System.Windows.Media.Color;

namespace PointUp.Wpf.Views;

public partial class FloatingBarWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly OverlayViewModel _viewModel;

    public FloatingBarWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        Left = SystemParameters.PrimaryScreenWidth / 2 - 120;
        Top = 24;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, style);

        UpdateInfiniteButton();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.IsLifetimeUnlimited))
            UpdateInfiniteButton();
    }

    private void UpdateInfiniteButton()
    {
        bool unlimited = _viewModel.IsLifetimeUnlimited;
        InfiniteButton.Foreground = unlimited
            ? new SolidColorBrush(Color.FromArgb(255, 255, 59, 48))
            : new SolidColorBrush(Color.FromArgb(204, 255, 255, 255));
        InfiniteButton.ToolTip = unlimited ? "∞ 線を消さない（クリックで通常に戻す）" : "∞ 線を消さない";
    }

    private void OnDragHandleDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnOffClick(object sender, RoutedEventArgs e)
        => _viewModel.ToggleCommand.Execute(null);

    private void OnClearClick(object sender, RoutedEventArgs e)
        => _viewModel.ClearCommand.Execute(null);

    private void OnInfiniteClick(object sender, RoutedEventArgs e)
        => _viewModel.ToggleLifetimeCommand.Execute(null);
}
