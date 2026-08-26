# Myria RPG — WPF Client

Myria is a fantasy RPG with turn-based combat, character classes and races, quests, jobs, crafting, guilds, player-run shops, and a rune-based magic system. This repository is the **Windows desktop client** (WPF, .NET 8, MVVM), one of several front ends built on the shared [Myria.Lib](https://github.com/MyriaGames/MyriaLib) game library. It can be played fully offline as a local single-player save, or online against a Myria realm server.

## Features

Grounded in the actual page/view-model structure of this client:

- **Character creation & progression** — race and class selection, stat growth, leveling, class re-selection.
- **Turn-based combat** — room encounters against monsters, both single-player and multiplayer (synced via SignalR).
- **Inventory & equipment** — item stacking, equipment slots, a money/currency system.
- **Quests** — active/completed quest tracking, repeatable quests, NPC quest dialogs.
- **Jobs & crafting** — job-based skill/knowledge/fame progression, crafting and upgrade panels at NPCs.
- **Skills** — skill slots, skill combination, and composite skill fusion.
- **Rune magic** — rune drawing, a rune lexicon/dictionary, and composable rune words.
- **Trading & shops** — NPC shops and player-to-player trading/player shops.
- **Guilds** — creation, membership ranks, invites, treasury, and guild property (houses/bases).
- **Social** — friends list, friend requests, blocking.
- **Multiplayer lobby** — a realm/lobby selection screen that queries each configured realm's live status and player count.
- **Mods** — a mod loader for both game-data and WPF asset mods (`Data/Mods/`).
- **Theming & localization** — light/dark themes and English/German localization, with WPF-specific UI strings layered on top of the shared locale files from Myria.Lib.
- **Auto-update** — checks for and installs client updates on startup (`Services/UpdateService.cs`, `Services/UpdateCoordinator.cs`).

## Architecture

Myria is split across several repositories under the [MyriaGames](https://github.com/MyriaGames) organization. This client contains almost no game logic itself — nearly everything (entities, rules, save/load models, data loading) lives in the shared library it references as a project reference.

- **[MyriaRPG](https://github.com/MyriaGames/MyriaRPG)** (this repo) — the WPF desktop client. UI, ViewModels, and app-level services (navigation, theming, settings, the HTTP/SignalR client for talking to the servers).
- **[MyriaLib](https://github.com/MyriaGames/MyriaLib)** — shared game library (`Myria.Lib.Core`), referenced by every client. Contains entities (characters, items, monsters, NPCs, maps), game systems, data loaders, and the game-content JSON/locale files.
- **[MyriaAuthServer](https://github.com/MyriaGames/MyriaAuthServer)** — the authentication server: account registration/login and the realm directory. Shared across all realms.
- **[MyriaServer](https://github.com/MyriaGames/MyriaServer)** — a realm/world server: game state, characters, guilds, and a SignalR hub for realtime multiplayer.
- **[ConsoleWorldRPG](https://github.com/MyriaGames/ConsoleWorldRPG)** — a text-console client built on the same Myria.Lib and backend.
- **[MyriaWorld](https://github.com/MyriaGames/MyriaWorld)** — a MonoGame-based client, currently in development, also built on Myria.Lib.

A player can either play entirely offline (a local, file-based save under `Data/users/` and `Data/saves/`, no server involved) or connect to a self-hosted or third-party Myria realm — the realm address is configurable per client, not hardcoded to one official server.

## Requirements

- Windows (WPF is Windows-only)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022+ (with the .NET desktop workload) or the `dotnet` CLI

This solution also expects a sibling clone of [MyriaLib](https://github.com/MyriaGames/MyriaLib) at `../Myria.Lib` relative to this repo, since `Myria.Wpf.csproj` references `..\Myria.Lib\Myria.Lib.Core\Myria.Lib.Core.csproj` and links in its `Data/common` and `Data/locales` content directly.

## Getting started

```bash
# Build the whole solution
dotnet build Myria.Wpf.sln

# Run the client
dotnet run

# Release build
dotnet build -c Release
```

On first launch you can either start **Single Player** (creates/loads a local save, no network access) or **Multiplayer**, which opens the login/registration screen against the configured Auth server. To connect to a specific realm, open **Settings → Game** and enter a server address; the client normalizes it (defaulting to `https://` for anything other than `localhost`/loopback) and persists it for future sessions. There is no config file to hand-edit for this.

TLS to a realm's self-signed certificate is handled trust-on-first-use, similar to an SSH host key: the first successful connection to a given host:port remembers that certificate's thumbprint, and later connections to the same address must present the same certificate. This lets the client connect to any self-hosted Myria server, not just one with a CA-issued certificate.

## Building a release installer

Maintainers can build a signed, self-contained Windows installer via `Setup/release.ps1`, which publishes the client, code-signs the executable and installer (via `Setup/myriaRPG_Setupscript.iss`, built with Inno Setup 6), and optionally publishes the servers alongside it. This requires a code-signing certificate already installed in the local certificate store (see `Setup/New-SigningCert.ps1`) and is not needed for local development or play.

## Configuration

- **Players**: the only thing you configure is the server/realm address, in-app under Settings → Game. Everything else (theme, language, auto-update, mods) is also configured through the in-app Settings pages and persisted automatically.
- **Maintainers**: release signing (certificate subject, Inno Setup path, releases repo location) is configured via parameters to `Setup/release.ps1`, not through any file players need to touch.

## Legal & Privacy

Myria RPG handles user accounts (username and password hash) via the Auth server, and is operated under Austrian law. The following documents in [`Legal/`](Legal/) apply (German-language, as required):

- [`Legal/Impressum.md`](Legal/Impressum.md) — the legally required operator disclosure (Impressum).
- [`Legal/Datenschutzerklaerung.md`](Legal/Datenschutzerklaerung.md) — the privacy policy (GDPR/DSGVO), describing what account data is processed and why.
- [`Legal/Nutzungsbedingungen.md`](Legal/Nutzungsbedingungen.md) — the terms of use governing account registration and use of the game.

## License

Licensed under the [MIT License](LICENSE).

## Status

Myria RPG is an active, non-commercial hobby project in alpha. As stated in the terms of use, there is no guarantee of uptime, error-free operation, or permanence of save data — servers (including the official one) can change, reset, or be discontinued without notice.
