using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AntiTiltTeemo.Models;
using AntiTiltTeemo.Services;

namespace AntiTiltTeemo.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan QueueLockDuration = TimeSpan.FromSeconds(30);
    private const int MatchHistoryLimit = 8;

    private readonly TiltManager _tiltManager;
    private readonly TeemoProgression _teemoProgression;
    private readonly DispatcherTimer _cooldownTimer;
    private TiltTier _tiltTier = AntiTiltTeemo.Models.TiltTier.CHILL;
    private long _totalHits;
    private long _mentalDamage;
    private int _cooldownSeconds;
    private bool _isQueueLocked;
    private string _queueStatus = "NOT QUEUED";
    private string _lastMatchResult = "DEFEAT";
    private string _audioStatus = "OPTIONAL HIT SOUNDS NOT INCLUDED";
    private string _teemoHitStatus = "CLICK TO ATTACK · HOVER A WEAPON FOR STATS";
    private Weapon? _selectedWeapon;

    public MainViewModel()
        : this(CreateDefaultDependencies())
    {
    }

    private MainViewModel((TiltManager TiltManager, TeemoProgression Progression) dependencies)
        : this(dependencies.TiltManager, dependencies.Progression)
    {
    }

    public MainViewModel(TiltManager tiltManager, TeemoProgression? teemoProgression = null)
    {
        _tiltManager = tiltManager ?? throw new ArgumentNullException(nameof(tiltManager));
        _teemoProgression = teemoProgression ?? new TeemoProgression();
        _teemoProgression.SaveFailed += OnProgressionSaveFailed;
        _tiltTier = _tiltManager.CurrentTier;
        TotalHits = _teemoProgression.TotalHits;
        MentalDamage = _teemoProgression.TotalDamage;
        Weapons = new ObservableCollection<Weapon>(_tiltManager.AllWeapons);
        SelectedWeapon = _tiltManager.UnlockedWeapons.Count > 0 ? _tiltManager.UnlockedWeapons[0] : null;
        RefreshWeaponOptions();

        _cooldownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _cooldownTimer.Tick += OnCooldownTimerTick;
    }

    public TiltTier TiltTier
    {
        get => _tiltTier;
        private set
        {
            if (SetProperty(ref _tiltTier, value))
            {
                OnPropertyChanged(nameof(TiltStatusColor));
                OnPropertyChanged(nameof(TiltStatusLabel));
                RefreshWeapons();
            }
        }
    }

    public string TiltStatusLabel => $"{TiltTier switch
    {
        AntiTiltTeemo.Models.TiltTier.CHILL => "CHILL",
        AntiTiltTeemo.Models.TiltTier.PEPPERED => "PEPPERED",
        AntiTiltTeemo.Models.TiltTier.TILTED => "TILTED",
        AntiTiltTeemo.Models.TiltTier.FRUSTRATED => "FRUSTRATED",
        AntiTiltTeemo.Models.TiltTier.INTING => "INTING",
        AntiTiltTeemo.Models.TiltTier.RAGING => "RAGING",
        AntiTiltTeemo.Models.TiltTier.FULL_PSYCHO => "FULL PSYCHO",
        _ => throw new ArgumentOutOfRangeException(nameof(TiltTier), TiltTier, "Invalid tilt tier.")
    }} · {(int)TiltTier}/6";

    public string TiltStatusColor => TiltTier switch
    {
        AntiTiltTeemo.Models.TiltTier.CHILL => "#164C78",
        AntiTiltTeemo.Models.TiltTier.PEPPERED => "#356B75",
        AntiTiltTeemo.Models.TiltTier.TILTED => "#665123",
        AntiTiltTeemo.Models.TiltTier.FRUSTRATED => "#80502A",
        AntiTiltTeemo.Models.TiltTier.INTING => "#943C2E",
        AntiTiltTeemo.Models.TiltTier.RAGING => "#9A2935",
        AntiTiltTeemo.Models.TiltTier.FULL_PSYCHO => "#762832",
        _ => throw new ArgumentOutOfRangeException(nameof(TiltTier), TiltTier, "Invalid tilt tier.")
    };

    public long TotalHits
    {
        get => _totalHits;
        private set => SetProperty(ref _totalHits, value);
    }

    public long MentalDamage
    {
        get => _mentalDamage;
        private set => SetProperty(ref _mentalDamage, value);
    }

    public int TeemoLevel => _teemoProgression.Level;

    public int TeemoCurrentHealth => _teemoProgression.CurrentHealth;

    public int TeemoMaximumHealth => _teemoProgression.MaximumHealth;

    public string TeemoHealthText =>
        $"{_teemoProgression.CurrentHealth} / {_teemoProgression.MaximumHealth} HP";

    public double TeemoHealthPercent =>
        (double)_teemoProgression.CurrentHealth / _teemoProgression.MaximumHealth * 100;

    public long TeemoKills => _teemoProgression.TotalKills;

    public int LastTeemoDamageDealt { get; private set; }

    public bool LastHitWasCritical { get; private set; }

    public bool HasTeemoSlayerAchievement => _teemoProgression.HasTeemoSlayerAchievement;

    public string TeemoSlayerStatus => HasTeemoSlayerAchievement
        ? "PERMANENTLY UNLOCKED"
        : "LOCKED · FIRST TEEMO KILL";

    public string TeemoSlayerStatusColor => HasTeemoSlayerAchievement ? "#F5D887" : "#77766F";

    public string TeemoHitStatus
    {
        get => _teemoHitStatus;
        private set => SetProperty(ref _teemoHitStatus, value);
    }

    public int CooldownSeconds
    {
        get => _cooldownSeconds;
        private set
        {
            if (SetProperty(ref _cooldownSeconds, value))
            {
                OnPropertyChanged(nameof(QueueButtonText));
            }
        }
    }

    public bool IsQueueLocked
    {
        get => _isQueueLocked;
        private set
        {
            if (SetProperty(ref _isQueueLocked, value))
            {
                RecordWinCommand.NotifyCanExecuteChanged();
                RecordLossCommand.NotifyCanExecuteChanged();
                ReturnToQueueCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(QueueButtonText));
            }
        }
    }

    public ObservableCollection<Weapon> Weapons { get; }

    public ObservableCollection<WeaponOptionViewModel> WeaponOptions { get; } = [];

    public ObservableCollection<MatchHistoryEntry> MatchHistory { get; } = [];

    public bool HasNoMatchHistory => MatchHistory.Count == 0;

    public Weapon? SelectedWeapon
    {
        get => _selectedWeapon;
        set
        {
            if (value is not null && !_tiltManager.UnlockedWeapons.Contains(value))
            {
                throw new InvalidOperationException($"Weapon '{value.Name}' is not currently unlocked.");
            }

            if (SetProperty(ref _selectedWeapon, value))
            {
                foreach (var option in WeaponOptions)
                {
                    option.IsSelected = ReferenceEquals(option.Weapon, value);
                }

                OnPropertyChanged(nameof(SelectedWeaponDamage));
                OnPropertyChanged(nameof(SelectedWeaponIcon));
                OnPropertyChanged(nameof(SelectedWeaponIconPath));
                OnPropertyChanged(nameof(SelectedWeaponDisplayName));
                HitTeemoCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int SelectedWeaponDamage => SelectedWeapon?.AttackDamage ?? 0;

    public string SelectedWeaponDisplayName => SelectedWeapon?.Name ?? "Empty Hand";

    public IImage? SelectedWeaponIcon
    {
        get => SelectedWeapon?.Icon;
    }

    public string? SelectedWeaponIconPath
    {
        get => SelectedWeapon?.IconPath;
    }

    public string LastMatchResult
    {
        get => _lastMatchResult;
        private set => SetProperty(ref _lastMatchResult, value);
    }

    public string QueueStatus
    {
        get => _queueStatus;
        private set => SetProperty(ref _queueStatus, value);
    }

    public string AudioStatus
    {
        get => _audioStatus;
        set => SetProperty(ref _audioStatus, value);
    }

    public string QueueButtonText => IsQueueLocked
        ? $"QUEUE LOCKED · {CooldownSeconds}s"
        : "RETURN TO QUEUE";

    [RelayCommand(CanExecute = nameof(CanRecordMatch))]
    private void RecordWin() => ProcessMatch(Outcome.WIN);

    [RelayCommand(CanExecute = nameof(CanRecordMatch))]
    private void RecordLoss() => ProcessMatch(Outcome.LOSS);

    [RelayCommand(CanExecute = nameof(CanHitTeemo))]
    private void HitTeemo()
    {
        if (SelectedWeapon is null)
        {
            throw new InvalidOperationException("Cannot hit Teemo without an unlocked weapon.");
        }

        var weapon = SelectedWeapon;
        var (rolledDamage, isCritical) = weapon.CalculateHit(Random.Shared.NextDouble());
        var hitResult = _teemoProgression.ApplyDamage(rolledDamage);
        _teemoProgression.RecordHit(hitResult.DamageDealt);
        _tiltManager.RegisterHit(weapon, hitResult.DamageDealt);
        TotalHits = _teemoProgression.TotalHits;
        MentalDamage = _teemoProgression.TotalDamage;
        OnPropertyChanged(nameof(TeemoLevel));
        OnPropertyChanged(nameof(TeemoCurrentHealth));
        OnPropertyChanged(nameof(TeemoMaximumHealth));
        OnPropertyChanged(nameof(TeemoHealthText));
        OnPropertyChanged(nameof(TeemoHealthPercent));
        OnPropertyChanged(nameof(TeemoKills));
        OnPropertyChanged(nameof(HasTeemoSlayerAchievement));
        OnPropertyChanged(nameof(TeemoSlayerStatus));
        OnPropertyChanged(nameof(TeemoSlayerStatusColor));
        LastTeemoDamageDealt = hitResult.DamageDealt;
        OnPropertyChanged(nameof(LastTeemoDamageDealt));
        LastHitWasCritical = isCritical;
        OnPropertyChanged(nameof(LastHitWasCritical));

        var hitStatus = hitResult.TeemoDefeated
            ? hitResult.AchievementUnlocked
                ? $"TEEMO DEFEATED · TEEMO SLAYER UNLOCKED · LEVEL {TeemoLevel}"
                : TeemoLevel == 18 && !hitResult.LeveledUp
                    ? "TEEMO DEFEATED · MAX LEVEL 18"
                    : $"TEEMO DEFEATED · LEVEL UP TO {TeemoLevel}"
            : $"HIT FOR {hitResult.DamageDealt} · {TeemoCurrentHealth} HP REMAINING";
        TeemoHitStatus = isCritical ? $"CRITICAL · {hitStatus}" : hitStatus;
    }

    [RelayCommand(CanExecute = nameof(CanReturnToQueue))]
    private void ReturnToQueue() => QueueStatus = "QUEUE REQUESTED";

    private bool CanRecordMatch() => !IsQueueLocked;

    private bool CanReturnToQueue() => !IsQueueLocked;

    private bool CanHitTeemo() => SelectedWeapon is not null;

    private void ProcessMatch(Outcome outcome)
    {
        var (currentTier, tierChanged) = _tiltManager.ProcessMatch(outcome);
        _teemoProgression.SaveMatchStreak(_tiltManager.StreakOutcome, _tiltManager.StreakLength);
        TiltTier = currentTier;
        LastMatchResult = outcome == Outcome.WIN ? "VICTORY" : "DEFEAT";
        MatchHistory.Insert(0, new MatchHistoryEntry(
            LastMatchResult,
            outcome == Outcome.WIN ? "#35C7A5" : "#E05252"));

        if (MatchHistory.Count > MatchHistoryLimit)
        {
            MatchHistory.RemoveAt(MatchHistory.Count - 1);
        }

        OnPropertyChanged(nameof(HasNoMatchHistory));

        if (tierChanged && currentTier == AntiTiltTeemo.Models.TiltTier.FULL_PSYCHO)
        {
            StartCooldown();
        }
    }

    private void StartCooldown()
    {
        CooldownSeconds = (int)QueueLockDuration.TotalSeconds;
        IsQueueLocked = true;
        _cooldownTimer.Start();
    }

    private void OnCooldownTimerTick(object? sender, EventArgs e)
    {
        if (CooldownSeconds > 1)
        {
            CooldownSeconds--;
            return;
        }

        _cooldownTimer.Stop();
        CooldownSeconds = 0;
        IsQueueLocked = false;
    }

    private void OnProgressionSaveFailed(object? sender, Exception e)
    {
        Dispatcher.UIThread.Post(() => AudioStatus = "PROGRESS SAVE FAILED");
    }

    private void RefreshWeapons()
    {
        var selectedWeapon = SelectedWeapon;
        var unlockedWeapons = _tiltManager.UnlockedWeapons;
        if (selectedWeapon is null || !unlockedWeapons.Contains(selectedWeapon))
        {
            SelectedWeapon = unlockedWeapons.Count > 0 ? unlockedWeapons[0] : null;
        }

        foreach (var option in WeaponOptions)
        {
            option.UpdateUnlockState(unlockedWeapons.Contains(option.Weapon));
        }
    }

    private void RefreshWeaponOptions()
    {
        var unlockedWeapons = _tiltManager.UnlockedWeapons;
        WeaponOptions.Clear();
        foreach (var weapon in Weapons)
        {
            WeaponOptions.Add(new WeaponOptionViewModel(
                weapon,
                selectedWeapon => SelectedWeapon = selectedWeapon,
                unlockedWeapons.Contains(weapon))
            {
                IsSelected = ReferenceEquals(weapon, SelectedWeapon)
            });
        }
    }

    public void Dispose()
    {
        _cooldownTimer.Stop();
        _cooldownTimer.Tick -= OnCooldownTimerTick;
        _teemoProgression.SaveFailed -= OnProgressionSaveFailed;
        _teemoProgression.Dispose();
    }

    private static (TiltManager TiltManager, TeemoProgression Progression) CreateDefaultDependencies()
    {
        var progression = new TeemoProgression();
        var playerState = new PlayerState(progression.StreakOutcome, progression.StreakLength);
        var tiltManager = new TiltManager(
            playerState,
            new Arsenal(),
            progression.TotalHits,
            progression.TotalDamage);
        return (tiltManager, progression);
    }
}
