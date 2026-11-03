using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using AntiTiltTeemo.Models;

namespace AntiTiltTeemo.Services;

public sealed class TeemoProgression : IDisposable
{
    private const int MaximumLevel = 18;
    private const double StartingHealth = 615;
    private const double HealthPerLevel = 104;
    private readonly string _savePath;
    private readonly object _saveLock = new();
    private readonly Timer _saveTimer;

    public TeemoProgression(string? savePath = null)
    {
        _savePath = savePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AntiTiltTeemo",
            "progression.json");

        var savedState = LoadState();
        Level = 1;
        TotalKills = savedState.TotalKills;
        HasTeemoSlayerAchievement = savedState.HasTeemoSlayerAchievement;
        TotalHits = savedState.TotalHits;
        TotalDamage = savedState.TotalDamage;
        StreakOutcome = savedState.StreakOutcome;
        StreakLength = savedState.StreakLength;
        CurrentHealth = MaximumHealth;
        _saveTimer = new Timer(SaveScheduledState, null, Timeout.Infinite, Timeout.Infinite);
    }

    public int Level { get; private set; }

    public int CurrentHealth { get; private set; }

    public int MaximumHealth
    {
        get
        {
            var levelGrowth = Level - 1;
            var growthMultiplier = 0.7025 + 0.0175 * levelGrowth;
            return (int)Math.Round(
                StartingHealth + HealthPerLevel * levelGrowth * growthMultiplier,
                MidpointRounding.AwayFromZero);
        }
    }

    public long TotalKills { get; private set; }

    public long TotalHits { get; private set; }

    public long TotalDamage { get; private set; }

    public Outcome? StreakOutcome { get; private set; }

    public int StreakLength { get; private set; }

    public bool HasTeemoSlayerAchievement { get; private set; }

    public void RecordHit(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

        lock (_saveLock)
        {
            TotalHits = checked(TotalHits + 1);
            TotalDamage = checked(TotalDamage + damage);
            _saveTimer.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        }
    }

    public void SaveMatchStreak(Outcome? streakOutcome, int streakLength)
    {
        if (streakLength < 0 ||
            (streakLength == 0 && streakOutcome is not null) ||
            (streakLength > 0 && streakOutcome is null) ||
            (streakOutcome is not null && streakOutcome is not Outcome.WIN and not Outcome.LOSS))
        {
            throw new ArgumentOutOfRangeException(nameof(streakLength), "Invalid match streak.");
        }

        lock (_saveLock)
        {
            SaveState(streakOutcome, streakLength);
            StreakOutcome = streakOutcome;
            StreakLength = streakLength;
        }
    }

    public TeemoHitResult ApplyDamage(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

        lock (_saveLock)
        {
            var damageDealt = Math.Min(damage, CurrentHealth);
            CurrentHealth -= damageDealt;
            if (CurrentHealth > 0)
            {
                return new TeemoHitResult(damageDealt, false, false, false);
            }

            TotalKills++;
            var achievementUnlocked = !HasTeemoSlayerAchievement;
            HasTeemoSlayerAchievement = true;
            var leveledUp = Level < MaximumLevel;
            if (leveledUp)
            {
                Level++;
            }

            CurrentHealth = MaximumHealth;
            _saveTimer.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
            return new TeemoHitResult(damageDealt, true, leveledUp, achievementUnlocked);
        }
    }

    private ProgressionSaveState LoadState()
    {
        if (!File.Exists(_savePath))
        {
            return new ProgressionSaveState();
        }

        var json = File.ReadAllText(_savePath);
        var state = JsonSerializer.Deserialize<ProgressionSaveState>(json)
            ?? throw new InvalidDataException($"The Teemo progression save file '{_savePath}' is empty or invalid.");

        if (state.TotalKills < 0 ||
            state.TotalHits < 0 ||
            state.TotalDamage < 0 ||
            (state.TotalHits > 0 && state.TotalDamage < state.TotalHits) ||
            state.StreakLength < 0 ||
            (state.StreakLength == 0 && state.StreakOutcome is not null) ||
            (state.StreakLength > 0 && state.StreakOutcome is null) ||
            (state.StreakOutcome is not null && state.StreakOutcome is not Outcome.WIN and not Outcome.LOSS))
        {
            throw new InvalidDataException($"The Teemo progression save file '{_savePath}' contains invalid values.");
        }

        return state;
    }

    private void SaveState()
    {
        lock (_saveLock)
        {
            SaveState(StreakOutcome, StreakLength);
        }
    }

    private void SaveState(Outcome? streakOutcome, int streakLength)
    {
        lock (_saveLock)
        {
            var directory = Path.GetDirectoryName(_savePath)
                ?? throw new InvalidOperationException("The progression save path must include a directory.");
            Directory.CreateDirectory(directory);

            var temporaryPath = $"{_savePath}.tmp";
            var state = new ProgressionSaveState
            {
                TotalKills = TotalKills,
                HasTeemoSlayerAchievement = HasTeemoSlayerAchievement,
                TotalHits = TotalHits,
                TotalDamage = TotalDamage,
                StreakOutcome = streakOutcome,
                StreakLength = streakLength
            };

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state));
            File.Move(temporaryPath, _savePath, overwrite: true);
        }
    }

    private void SaveScheduledState(object? state)
    {
        try
        {
            SaveState();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            System.Diagnostics.Trace.TraceError($"Unable to save Teemo progression: {exception}");
            SaveFailed?.Invoke(this, exception);
        }
    }

    public event EventHandler<Exception>? SaveFailed;

    public void Dispose()
    {
        _saveTimer.Dispose();
        SaveState();
    }

    private sealed class ProgressionSaveState
    {
        public ProgressionSaveState()
        {
        }

        public long TotalKills { get; set; }

        public bool HasTeemoSlayerAchievement { get; set; }

        public long TotalHits { get; set; }

        public long TotalDamage { get; set; }

        public Outcome? StreakOutcome { get; set; }

        public int StreakLength { get; set; }
    }
}

public readonly record struct TeemoHitResult(
    int DamageDealt,
    bool TeemoDefeated,
    bool LeveledUp,
    bool AchievementUnlocked);
