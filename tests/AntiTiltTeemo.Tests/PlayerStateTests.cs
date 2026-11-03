using AntiTiltTeemo.Models;
using Xunit;

namespace AntiTiltTeemo.Tests;

public sealed class PlayerStateTests
{
    [Fact]
    public void ConsecutiveLossesIncreaseTiltTier()
    {
        var state = new PlayerState();

        state.RecordMatch(Outcome.LOSS);
        Assert.Equal(TiltTier.PEPPERED, state.TiltTier);

        state.RecordMatch(Outcome.LOSS);
        Assert.Equal(TiltTier.TILTED, state.TiltTier);
        Assert.Equal(2, state.LossStreakLength);
    }

    [Fact]
    public void WinResetsLossStreakAndTiltTier()
    {
        var state = new PlayerState(Outcome.LOSS, 4);

        state.RecordMatch(Outcome.WIN);

        Assert.Equal(TiltTier.CHILL, state.TiltTier);
        Assert.Equal(0, state.LossStreakLength);
        Assert.Equal(Outcome.WIN, state.StreakOutcome);
        Assert.Equal(1, state.StreakLength);
    }

    [Fact]
    public void LossStreakCapsAtFullPsycho()
    {
        var state = new PlayerState();

        for (var index = 0; index < 10; index++)
        {
            state.RecordMatch(Outcome.LOSS);
        }

        Assert.Equal(TiltTier.FULL_PSYCHO, state.TiltTier);
        Assert.Equal(10, state.LossStreakLength);
    }
}
