using PointUp.Application.UseCases;

namespace PointUp.Tests.Application;

public class CalculateVelocityThicknessUseCaseTests
{
    private readonly CalculateVelocityThicknessUseCase _sut = new();

    // smoothing=1.0 で平滑化なし（即時反映）
    private double Calc(double speed, double prev = 5.0) =>
        _sut.Calculate(speed,
            minThickness: 1.5, maxThickness: 9.0,
            velocityAtMinThickness: 3.0, velocityAtMaxThickness: 0.2,
            previousThickness: prev, smoothingFactor: 1.0);

    [Fact]
    public void FastSpeed_ReturnsMinThickness()
    {
        var result = Calc(10.0); // velocityAtMinThickness=3.0 を大きく超える
        Assert.Equal(1.5, result);
    }

    [Fact]
    public void SlowSpeed_ReturnsMaxThickness()
    {
        var result = Calc(0.0); // velocityAtMaxThickness=0.2 より遅い
        Assert.Equal(9.0, result);
    }

    [Fact]
    public void MidSpeed_ReturnsMidThickness()
    {
        // speed=1.6: t=(1.6-0.2)/(3.0-0.2)=0.5 → thickness=9.0-0.5*7.5=5.25
        var result = Calc(1.6);
        Assert.InRange(result, 5.2, 5.3);
    }

    [Fact]
    public void Smoothing_AppliesEma()
    {
        // smoothing=0.5, prev=9.0, target=1.5(fast) → 9.0+0.5*(1.5-9.0)=5.25
        var result = _sut.Calculate(10.0, 1.5, 9.0, 3.0, 0.2, 9.0, 0.5);
        Assert.InRange(result, 5.2, 5.3);
    }

    [Fact]
    public void VelocityRangeZero_ReturnsMiddle()
    {
        // velocityAtMin==velocityAtMax → 中間値を返す
        var result = _sut.Calculate(1.0, 2.0, 8.0, 1.0, 1.0, 5.0, 1.0);
        Assert.Equal(5.0, result);
    }
}
