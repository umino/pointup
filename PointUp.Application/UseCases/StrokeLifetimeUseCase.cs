namespace PointUp.Application.UseCases;

public class StrokeLifetimeUseCase
{
    /// <summary>
    /// ストロークの年齢から不透明度と削除フラグを計算する。
    /// LifetimeMs 経過後にフェードアウト開始、FadeMs 後に削除。
    /// </summary>
    /// <param name="ageMs">ストローク完了からの経過時間(ms)</param>
    /// <param name="lifetimeMs">フェード開始までの時間(ms)</param>
    /// <param name="fadeMs">フェードの所要時間(ms)</param>
    /// <returns>(不透明度 0.0–1.0, 削除フラグ)</returns>
    public (double Opacity, bool Expired) Calculate(long ageMs, int lifetimeMs, int fadeMs)
    {
        if (ageMs < lifetimeMs)
            return (1.0, false);

        if (fadeMs <= 0)
            return (0.0, true);

        long fadeAge = ageMs - lifetimeMs;
        if (fadeAge >= fadeMs)
            return (0.0, true);

        double opacity = 1.0 - (double)fadeAge / fadeMs;
        return (opacity, false);
    }
}
