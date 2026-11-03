using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AntiTiltTeemo.Models;

public sealed class Weapon
{
    public Weapon(
        string name,
        string iconPath,
        int attackDamage,
        string hitSoundAsset,
        TiltTier unlockTier,
        string leagueStats,
        string passive,
        double criticalStrikeChance = 0,
        double criticalStrikeDamageBonus = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attackDamage);
        ArgumentException.ThrowIfNullOrWhiteSpace(hitSoundAsset);
        ArgumentException.ThrowIfNullOrWhiteSpace(leagueStats);
        ArgumentException.ThrowIfNullOrWhiteSpace(passive);
        ArgumentOutOfRangeException.ThrowIfNegative(criticalStrikeChance);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(criticalStrikeChance, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(criticalStrikeDamageBonus);

        Name = name;
        IconPath = iconPath;
        var iconUri = new Uri(iconPath, UriKind.RelativeOrAbsolute);
        Icon = AssetLoader.Exists(iconUri) ? new Bitmap(AssetLoader.Open(iconUri)) : null;
        AttackDamage = attackDamage;
        HitSoundAsset = hitSoundAsset;
        UnlockTier = unlockTier;
        LeagueStats = leagueStats;
        Passive = passive;
        CriticalStrikeChance = criticalStrikeChance;
        CriticalStrikeDamageBonus = criticalStrikeDamageBonus;
    }

    public string Name { get; }

    public string IconPath { get; }

    public IImage? Icon { get; }

    public int AttackDamage { get; }

    public string HitSoundAsset { get; }

    public TiltTier UnlockTier { get; }

    public string LeagueStats { get; }

    public string Passive { get; }

    public double CriticalStrikeChance { get; }

    public double CriticalStrikeDamageBonus { get; }

    public double CriticalStrikeDamageMultiplier => 1.75 + CriticalStrikeDamageBonus;

    public (int Damage, bool IsCritical) CalculateHit(double criticalRoll)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(criticalRoll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(criticalRoll, 1);

        var isCritical = criticalRoll < CriticalStrikeChance;
        var damage = isCritical
            ? (int)Math.Round(AttackDamage * CriticalStrikeDamageMultiplier, MidpointRounding.AwayFromZero)
            : AttackDamage;

        return (damage, isCritical);
    }
}
