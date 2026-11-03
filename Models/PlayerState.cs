using System;
using System.Collections.Generic;

namespace AntiTiltTeemo.Models;

public enum TiltTier
{
    CHILL = 0,
    PEPPERED = 1,
    TILTED = 2,
    FRUSTRATED = 3,
    INTING = 4,
    RAGING = 5,
    FULL_PSYCHO = 6
}

public enum Outcome
{
    WIN,
    LOSS
}

public sealed class PlayerState
{
    private readonly List<Outcome> _matchHistory = [];

    public PlayerState(Outcome? streakOutcome = null, int streakLength = 0)
    {
        if (streakLength < 0 ||
            (streakLength == 0 && streakOutcome is not null) ||
            (streakLength > 0 && streakOutcome is null) ||
            (streakOutcome is not null && streakOutcome is not Outcome.WIN and not Outcome.LOSS))
        {
            throw new ArgumentOutOfRangeException(nameof(streakLength), "Invalid saved match streak.");
        }

        StreakOutcome = streakOutcome;
        StreakLength = streakLength;
        UpdateTiltTier();
    }

    public IReadOnlyList<Outcome> MatchHistory => _matchHistory.AsReadOnly();

    public int StreakLength { get; private set; }

    public Outcome? StreakOutcome { get; private set; }

    public int LossStreakLength => StreakOutcome == Outcome.LOSS ? StreakLength : 0;

    public TiltTier TiltTier { get; private set; } = TiltTier.CHILL;

    public void RecordMatch(Outcome outcome)
    {
        if (outcome is not Outcome.WIN and not Outcome.LOSS)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Invalid match outcome.");
        }

        _matchHistory.Add(outcome);

        if (StreakOutcome == outcome)
        {
            StreakLength++;
        }
        else
        {
            StreakOutcome = outcome;
            StreakLength = 1;
        }

        UpdateTiltTier();
    }

    private void UpdateTiltTier()
    {
        TiltTier = Math.Min(LossStreakLength, (int)TiltTier.FULL_PSYCHO) switch
        {
            0 => TiltTier.CHILL,
            1 => TiltTier.PEPPERED,
            2 => TiltTier.TILTED,
            3 => TiltTier.FRUSTRATED,
            4 => TiltTier.INTING,
            5 => TiltTier.RAGING,
            _ => TiltTier.FULL_PSYCHO
        };
    }
}
