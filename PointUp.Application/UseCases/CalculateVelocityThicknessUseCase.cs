namespace PointUp.Application.UseCases;

public class CalculateVelocityThicknessUseCase
{
    /// <summary>
    /// マウス速度(px/ms)から線の太さを計算する。速い=細い、遅い=太い（逆相関）。
    /// </summary>
    /// <param name="speedPxPerMs">現在のマウス速度</param>
    /// <param name="minThickness">最小太さ（最速時）</param>
    /// <param name="maxThickness">最大太さ（最遅時）</param>
    /// <param name="velocityAtMinThickness">この速度以上で最小太さ</param>
    /// <param name="velocityAtMaxThickness">この速度以下で最大太さ</param>
    /// <param name="previousThickness">直前の太さ（EMA 平滑化のため）</param>
    /// <param name="smoothingFactor">EMA 係数 (0=変化なし, 1=即時反映)</param>
    public double Calculate(
        double speedPxPerMs,
        double minThickness,
        double maxThickness,
        double velocityAtMinThickness,
        double velocityAtMaxThickness,
        double previousThickness,
        double smoothingFactor)
    {
        double range = velocityAtMinThickness - velocityAtMaxThickness;
        double targetThickness;
        if (Math.Abs(range) < 1e-6)
        {
            targetThickness = (minThickness + maxThickness) / 2.0;
        }
        else
        {
            // t: 0=遅い(max thick) → 1=速い(min thick)
            double t = (speedPxPerMs - velocityAtMaxThickness) / range;
            t = Math.Clamp(t, 0.0, 1.0);
            targetThickness = maxThickness - t * (maxThickness - minThickness);
        }

        double smoothed = previousThickness + smoothingFactor * (targetThickness - previousThickness);
        return Math.Clamp(smoothed, minThickness, maxThickness);
    }
}
