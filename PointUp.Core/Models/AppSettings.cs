namespace PointUp.Core.Models;

public class AppSettings
{
    public string LineColor { get; set; } = "#FFFF3B30";
    public double Thickness { get; set; } = 4.0;
    public int LifetimeMs { get; set; } = 1500;
    public int FadeMs { get; set; } = 500;
    public bool VelocityThicknessEnabled { get; set; } = false;
    public double MinThickness { get; set; } = 1.5;
    public double MaxThickness { get; set; } = 9.0;
    /// <summary>この速度(px/ms)以上で最も細い線になる</summary>
    public double VelocityAtMinThickness { get; set; } = 3.0;
    /// <summary>この速度(px/ms)以下で最も太い線になる</summary>
    public double VelocityAtMaxThickness { get; set; } = 0.2;
    /// <summary>EMA 平滑化係数 (0=変化なし, 1=即時)</summary>
    public double SmoothingFactor { get; set; } = 0.3;
    /// <summary>起動時にポインティングを ON にするか</summary>
    public bool StartEnabled { get; set; } = false;
    public ShortcutDefinition ToggleShortcut { get; set; } = new() { Ctrl = true, Shift = true, Key = "D" };
    public ShortcutDefinition ClearShortcut { get; set; } = new() { Ctrl = true, Shift = true, Key = "C" };
}
