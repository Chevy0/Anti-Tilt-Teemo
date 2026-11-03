using System;
using System.Collections.Generic;

namespace AntiTiltTeemo.Models;

public sealed class Arsenal
{
    private static readonly Weapon InfinityEdge = new(
        "Infinity Edge",
        "avares://AntiTiltTeemo/Assets/Weapons/3031.png",
        75,
        "Sounds/critical-hit-sounds-effect.mp3",
        TiltTier.INTING,
        "75 Attack Damage\n25% Critical Strike Chance\n30% Critical Strike Damage",
        "Crits deal 205% total damage.",
        0.25,
        0.30);
    private static readonly Weapon Bloodthirster = new(
        "Bloodthirster",
        "avares://AntiTiltTeemo/Assets/Weapons/3072.png",
        80,
        "Sounds/punch_u4LmMsr.mp3",
        TiltTier.FULL_PSYCHO,
        "80 Attack Damage\n15% Life Steal",
        "Excess life steal healing converts into a shield.");
    private static readonly Weapon GuinsoosRageblade = new(
        "Guinsoo's Rageblade",
        "avares://AntiTiltTeemo/Assets/Weapons/3124.png",
        30,
        "Sounds/bongo-hit.mp3",
        TiltTier.PEPPERED,
        "30 Attack Damage\n30 Ability Power\n25% Attack Speed",
        "Attacks deal 30 bonus magic damage. Attack speed stacks on hit.");
    private static readonly Weapon BladeOfTheRuinedKing = new(
        "Blade of the Ruined King",
        "avares://AntiTiltTeemo/Assets/Weapons/3153.png",
        40,
        "Sounds/bat_hit.mp3",
        TiltTier.FRUSTRATED,
        "40 Attack Damage\n25% Attack Speed\n10% Life Steal",
        "Attacks deal bonus physical damage based on the target's current health.");
    private static readonly Weapon DoransBlade = new(
        "Doran's Blade",
        "avares://AntiTiltTeemo/Assets/Weapons/1055.png",
        10,
        "Sounds/cartoon-hammer.mp3",
        TiltTier.CHILL,
        "10 Attack Damage\n80 Health\n2.5% Omnivamp",
        "A starter blade with a little extra sustain.");
    private static readonly Weapon MortalReminder = new(
        "Mortal Reminder",
        "avares://AntiTiltTeemo/Assets/Weapons/3033.png",
        35,
        "Sounds/hitsound_2.mp3",
        TiltTier.TILTED,
        "35 Attack Damage\n30% Armor Penetration\n25% Critical Strike Chance",
        "Physical damage applies 40% Grievous Wounds for 3 seconds.",
        0.25);

    private static readonly IReadOnlyList<Weapon> AllWeapons = Array.AsReadOnly(
        [DoransBlade, GuinsoosRageblade, MortalReminder, BladeOfTheRuinedKing, InfinityEdge, Bloodthirster]);
    private static readonly IReadOnlyList<Weapon> ChillWeapons = Array.AsReadOnly([DoransBlade]);
    private static readonly IReadOnlyList<Weapon> PepperedWeapons =
        Array.AsReadOnly([DoransBlade, GuinsoosRageblade]);
    private static readonly IReadOnlyList<Weapon> TiltedWeapons =
        Array.AsReadOnly([DoransBlade, GuinsoosRageblade, MortalReminder]);
    private static readonly IReadOnlyList<Weapon> FrustratedWeapons =
        Array.AsReadOnly([DoransBlade, GuinsoosRageblade, MortalReminder, BladeOfTheRuinedKing]);
    private static readonly IReadOnlyList<Weapon> IntingWeapons =
        Array.AsReadOnly([DoransBlade, GuinsoosRageblade, MortalReminder, BladeOfTheRuinedKing, InfinityEdge]);
    private static readonly IReadOnlyList<Weapon> RagingWeapons =
        Array.AsReadOnly([DoransBlade, GuinsoosRageblade, MortalReminder, BladeOfTheRuinedKing, InfinityEdge]);

    public IReadOnlyList<Weapon> GetAllWeapons() => AllWeapons;

    public IReadOnlyList<Weapon> GetUnlockedWeapons(TiltTier tiltTier) => tiltTier switch
    {
        TiltTier.CHILL => ChillWeapons,
        TiltTier.PEPPERED => PepperedWeapons,
        TiltTier.TILTED => TiltedWeapons,
        TiltTier.FRUSTRATED => FrustratedWeapons,
        TiltTier.INTING => IntingWeapons,
        TiltTier.RAGING => RagingWeapons,
        TiltTier.FULL_PSYCHO => AllWeapons,
        _ => throw new ArgumentOutOfRangeException(nameof(tiltTier), tiltTier, "Invalid tilt tier.")
    };
}
