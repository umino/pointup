using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PointUp.Wpf.ViewModels;

namespace PointUp.Wpf.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly OverlayViewModel _viewModel;
    private readonly ToolStripMenuItem _toggleMenuItem;

    public TrayIconService(OverlayViewModel viewModel)
    {
        _viewModel = viewModel;

        _toggleMenuItem = new ToolStripMenuItem();
        _toggleMenuItem.Click += (_, _) => _viewModel.ToggleCommand.Execute(null);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleMenuItem);
        menu.Items.Add("全消去 (Ctrl+Shift+C)", null, (_, _) => _viewModel.ClearCommand.Execute(null));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("設定ファイルを開く", null, (_, _) => _viewModel.OpenSettingsFileCommand.Execute(null));
        menu.Items.Add("設定再読込", null, (_, _) => _viewModel.ReloadSettingsCommand.Execute(null));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _notifyIcon = new NotifyIcon
        {
            Text = "point up",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _viewModel.ToggleCommand.Execute(null);

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateMenuState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.IsPointingEnabled))
            UpdateMenuState();
    }

    private void UpdateMenuState()
    {
        bool isOn = _viewModel.IsPointingEnabled;
        _toggleMenuItem.Text = isOn
            ? "描画 ON  ← クリックで OFF (Ctrl+Shift+D)"
            : "描画 OFF ← クリックで ON  (Ctrl+Shift+D)";
        _notifyIcon.Text = isOn ? "point up — 描画中" : "point up — 待機中";
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.FillEllipse(new SolidBrush(Color.FromArgb(255, 255, 59, 48)), 1, 1, 14, 14);
        g.DrawEllipse(new Pen(Color.White, 1.5f), 2, 2, 12, 12);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
