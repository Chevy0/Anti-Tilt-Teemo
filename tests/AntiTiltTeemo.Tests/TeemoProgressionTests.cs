using System;
using System.IO;
using AntiTiltTeemo.Models;
using AntiTiltTeemo.Services;
using Xunit;

namespace AntiTiltTeemo.Tests;

public sealed class TeemoProgressionTests
{
    [Fact]
    public void DefeatingTeemoUnlocksAchievementAndLevelsUp()
    {
        var directory = CreateSaveDirectory();
        try
        {
            using var progression = new TeemoProgression(Path.Combine(directory, "progression.json"));

            var result = progression.ApplyDamage(progression.CurrentHealth);

            Assert.True(result.TeemoDefeated);
            Assert.True(result.LeveledUp);
            Assert.True(result.AchievementUnlocked);
            Assert.Equal(2, progression.Level);
            Assert.Equal(1, progression.TotalKills);
            Assert.True(progression.HasTeemoSlayerAchievement);
            Assert.Equal(progression.MaximumHealth, progression.CurrentHealth);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DamageIsCappedAtRemainingHealth()
    {
        var directory = CreateSaveDirectory();
        try
        {
            using var progression = new TeemoProgression(Path.Combine(directory, "progression.json"));

            var result = progression.ApplyDamage(progression.CurrentHealth + 50);

            Assert.Equal(615, result.DamageDealt);
            Assert.True(result.TeemoDefeated);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MatchStreakPersistsAcrossInstances()
    {
        var directory = CreateSaveDirectory();
        var savePath = Path.Combine(directory, "progression.json");
        try
        {
            using (var progression = new TeemoProgression(savePath))
            {
                progression.SaveMatchStreak(Outcome.LOSS, 3);
            }

            using var reloaded = new TeemoProgression(savePath);

            Assert.Equal(Outcome.LOSS, reloaded.StreakOutcome);
            Assert.Equal(3, reloaded.StreakLength);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateSaveDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AntiTiltTeemo.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
