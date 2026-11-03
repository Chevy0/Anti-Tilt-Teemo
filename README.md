# AntiTiltTeemo

A Windows desktop game companion built with C# and Avalonia that turns match streaks into a tilt meter, weapon progression, and a Teemo-hitting minigame.

## Features

- Record wins and losses, track streaks, and unlock escalating tilt tiers.
- Equip game-inspired weapon loadouts with damage and critical-hit stats.
- Play a click-to-hit minigame with health, levels, kills, and an achievement.
- Save progression locally between sessions.
- Optionally load local weapon icons and hit sounds when you have verified permission to use them.

This is not a desktop-pet app: it does not currently include custom pets, per-action animations, a pet model viewer, or a settings UI.

## Screenshot

Screenshot/GIF not included yet. Add a screenshot of the running application here after capturing it on Windows.

## Requirements

- Windows
- .NET 10 SDK

## Build and run

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet run
```

Run the unit tests with:

```powershell
dotnet test tests\AntiTiltTeemo.Tests\AntiTiltTeemo.Tests.csproj
```

## Optional local assets

Optional media is not included in the repository. Add only assets that you have permission to use; the `Assets/` directory is ignored by Git.

```text
Assets/
  Weapons/    Optional weapon icons
  Sounds/     Optional MP3 hit sounds
  Pets/       Reserved for possible future work; no pet viewer is implemented
```

`Assets.sample/` contains notes about these folders. The app can run without optional media.

## What I learned / what I would improve

- Keep match state, progression, and weapon behavior in separate model and service classes.
- Validate and persist local save data instead of treating it as trusted input.
- Add a real settings experience and automated UI coverage.
- Add a pet or animation system only after defining its scope and verifying the licenses for any assets.

## License and credits

The application source is available under the MIT License. See [LICENSE](LICENSE) and [CREDITS.md](CREDITS.md) for software and asset attribution.
