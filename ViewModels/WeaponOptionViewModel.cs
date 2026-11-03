using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AntiTiltTeemo.Models;

namespace AntiTiltTeemo.ViewModels;

public partial class WeaponOptionViewModel : ObservableObject
{
    private readonly Action<Weapon> _equipWeapon;
    private bool _isSelected;
    private bool _isUnlocked;
    private string _unlockRequirement;
    private string _unlockRequirementColor;

    public WeaponOptionViewModel(Weapon weapon, Action<Weapon> equipWeapon, bool isUnlocked)
    {
        Weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
        _equipWeapon = equipWeapon ?? throw new ArgumentNullException(nameof(equipWeapon));
        _isUnlocked = isUnlocked;
        _unlockRequirement = isUnlocked ? "AVAILABLE" : GetUnlockRequirement(weapon.UnlockTier);
        _unlockRequirementColor = isUnlocked ? "#35C7A5" : "#E05252";
    }

    public Weapon Weapon { get; }

    public string UnlockRequirement
    {
        get => _unlockRequirement;
        private set => SetProperty(ref _unlockRequirement, value);
    }

    public string UnlockRequirementColor
    {
        get => _unlockRequirementColor;
        private set => SetProperty(ref _unlockRequirementColor, value);
    }

    public string StatsTooltip =>
        $"{Weapon.Name}\n{Weapon.LeagueStats}\n\n{Weapon.Passive}\n\n" +
        (Weapon.CriticalStrikeChance > 0
            ? $"Critical Strike Chance: {Weapon.CriticalStrikeChance:P0}\n"
            : string.Empty) +
        (Weapon.CriticalStrikeDamageBonus > 0
            ? $"Critical Strike Damage: +{Weapon.CriticalStrikeDamageBonus:P0}\n"
            : string.Empty) +
        (IsUnlocked ? "Click to equip" : UnlockRequirement);

    public bool IsUnlocked
    {
        get => _isUnlocked;
        private set
        {
            if (SetProperty(ref _isUnlocked, value))
            {
                OnPropertyChanged(nameof(CardOpacity));
                OnPropertyChanged(nameof(StatsTooltip));
                EquipCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public double CardOpacity => IsUnlocked ? 1 : 0.42;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void UpdateUnlockState(bool isUnlocked)
    {
        UnlockRequirement = isUnlocked ? "AVAILABLE" : GetUnlockRequirement(Weapon.UnlockTier);
        UnlockRequirementColor = isUnlocked ? "#35C7A5" : "#E05252";
        OnPropertyChanged(nameof(StatsTooltip));
        IsUnlocked = isUnlocked;
    }

    public string AttackStatsText => Weapon.CriticalStrikeChance > 0
        ? $"{Weapon.AttackDamage} AD · {Weapon.CriticalStrikeChance:P0} CRIT"
        : $"{Weapon.AttackDamage} ATTACK DAMAGE";

    [RelayCommand(CanExecute = nameof(IsUnlocked))]
    private void Equip() => _equipWeapon(Weapon);

    private static string GetUnlockRequirement(TiltTier tier) => tier switch
    {
        TiltTier.CHILL => "UNLOCKED",
        TiltTier.PEPPERED => "UNLOCK AT 1 LOSS",
        TiltTier.TILTED => "UNLOCK AT 2 LOSSES",
        TiltTier.FRUSTRATED => "UNLOCK AT 3 LOSSES",
        TiltTier.INTING => "UNLOCK AT 4 LOSSES",
        TiltTier.RAGING => "UNLOCK AT 5 LOSSES",
        TiltTier.FULL_PSYCHO => "UNLOCK AT 6 LOSSES",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Invalid weapon unlock tier.")
    };
}
