using System;
using System.Collections.Generic;
using System.Linq;
using AntiTiltTeemo.Models;

namespace AntiTiltTeemo.Services;

public sealed class TiltManager
{
    private readonly PlayerState _playerState;
    private readonly Arsenal _arsenal;

    public TiltManager(
        PlayerState playerState,
        Arsenal arsenal,
        long totalTeemoHits = 0,
        long totalMentalDamage = 0)
    {
        _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        _arsenal = arsenal ?? throw new ArgumentNullException(nameof(arsenal));
        ArgumentOutOfRangeException.ThrowIfNegative(totalTeemoHits);
        ArgumentOutOfRangeException.ThrowIfNegative(totalMentalDamage);
        TotalTeemoHits = totalTeemoHits;
        TotalMentalDamage = totalMentalDamage;
    }

    public long TotalTeemoHits { get; private set; }

    public long TotalMentalDamage { get; private set; }

    public Outcome? StreakOutcome => _playerState.StreakOutcome;

    public int StreakLength => _playerState.StreakLength;

    public TiltTier CurrentTier => _playerState.TiltTier;

    public IReadOnlyList<Weapon> AllWeapons => _arsenal.GetAllWeapons();

    public IReadOnlyList<Weapon> UnlockedWeapons =>
        _arsenal.GetUnlockedWeapons(_playerState.TiltTier);

    public (TiltTier CurrentTier, bool TierChanged) ProcessMatch(Outcome outcome)
    {
        var previousTier = _playerState.TiltTier;
        _playerState.RecordMatch(outcome);

        var currentTier = _playerState.TiltTier;
        return (currentTier, currentTier != previousTier);
    }

    public void RegisterHit(Weapon weapon, int damageDealt)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damageDealt);
        if (!UnlockedWeapons.Contains(weapon))
        {
            throw new InvalidOperationException($"Weapon '{weapon.Name}' is not unlocked at the current tilt tier.");
        }

        var totalTeemoHits = checked(TotalTeemoHits + 1);
        var totalMentalDamage = checked(TotalMentalDamage + damageDealt);
        TotalTeemoHits = totalTeemoHits;
        TotalMentalDamage = totalMentalDamage;
    }
}
