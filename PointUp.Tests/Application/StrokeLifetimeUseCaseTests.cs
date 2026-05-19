using PointUp.Application.UseCases;

namespace PointUp.Tests.Application;

public class StrokeLifetimeUseCaseTests
{
    private readonly StrokeLifetimeUseCase _sut = new();

    [Fact]
    public void WithinLifetime_ReturnsFullOpacity()
    {
        var (opacity, expired) = _sut.Calculate(500, lifetimeMs: 1500, fadeMs: 500);
        Assert.Equal(1.0, opacity);
        Assert.False(expired);
    }

    [Fact]
    public void AtLifetimeBoundary_FullOpacity()
    {
        var (opacity, expired) = _sut.Calculate(1500, lifetimeMs: 1500, fadeMs: 500);
        Assert.Equal(1.0, opacity);
        Assert.False(expired);
    }

    [Fact]
    public void MidFade_ReturnsHalfOpacity()
    {
        // age=1750, lifetime=1500, fade=500 → fadeAge=250 → opacity=1-250/500=0.5
        var (opacity, expired) = _sut.Calculate(1750, lifetimeMs: 1500, fadeMs: 500);
        Assert.InRange(opacity, 0.49, 0.51);
        Assert.False(expired);
    }

    [Fact]
    public void AfterFade_Expired()
    {
        var (_, expired) = _sut.Calculate(2001, lifetimeMs: 1500, fadeMs: 500);
        Assert.True(expired);
    }

    [Fact]
    public void AtFadeEnd_Expired()
    {
        var (opacity, expired) = _sut.Calculate(2000, lifetimeMs: 1500, fadeMs: 500);
        Assert.Equal(0.0, opacity);
        Assert.True(expired);
    }

    [Fact]
    public void ZeroFadeMs_InstantExpiry()
    {
        var (_, expired) = _sut.Calculate(1501, lifetimeMs: 1500, fadeMs: 0);
        Assert.True(expired);
    }
}
