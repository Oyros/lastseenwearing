# Status

**Read this first in any new session.** Where the project is, what is in flight, what is
blocking. Shared and committed — personal task lists go in `TODO.md`.

Keep it short. A snapshot, not a log; finished phases move to §4 as one line.

---

## 1 · Right now

| | |
|---|---|
| **Phase** | **P0 — Scaffold** (`docs/ROADMAP.md`) — open, build, connect two players |
| **In flight** | Nothing |
| **Next** | `P0.09` — Windows build script (Editor menu), Addressables content included (`/next`) |
| **Blocking** | Nothing — UnityMCP connected; Blender MCP timed out this session (only matters for PL work) |
| **Parallel** | **PL — Look-dev & art**: `PL.00`, `PL.02`, `PL.03`, `PL.05`, `PL.20`, `PL.21` done (references, check/CCTV/ref-loader tools, greybox kit, layouts A–D). Next: `PL.06` self-test in this machine's Blender, then `PL.01` palette and `PL.04` export, then `PL.07` the male base body |
| **Unity** | 6000.5.7f1 · URP · Netcode for GameObjects + Facepunch transport (listen server over Steam) |

## 2 · Done in this phase

- `P0.01a` — docs, commands, git config files and `Packages/manifest.json` mirrored from Borrowed Crown / Pane & Panic (D-001).
- `P0.01` — `Assets/_Project/` tree (Borrowed Crown layout) and asmdefs `LastSeenWearing.Core/Gameplay/UI/Editor/Tests` through Unity MCP; all references resolve.
- `P0.02` — opens in 6000.5.7f1, packages resolve, console clean.
- `P0.03` — git repo (`main` + `development`), LFS via `.gitattributes`, first commit; empty `_Project` folders carry `.keep` so their `.meta` files survive a clone.
- `P0.04` — `GameConfig` (`Core/Config`) and `Data/Config/GameConfig.asset`; empty until the first domain config lands.
- `P0.05` — `Bootstrap.unity` (only `GameBootstrap`: holds `GameConfig`, loads the first scene) and `Sandbox_Empty.unity`; build list Bootstrap → Sandbox; a Windows dev build runs clean.
- `P0.06` — `Settings/LastSeenWearingControls.inputactions` (`Field` 10 actions, `Watcher` 12; KeyboardMouse + Gamepad) and the generated `Gameplay/Player/LastSeenWearingControls`; bindings fixed in DATA.md §7 (D-014).
- `P0.07` — Localization settings, `en` (startup locale), tables `UI`/`Roles`/`Festival`, TMP Essentials; `ui.menu.title` rendered through TMP + `LocalizeStringEvent` in play mode (LOCALIZATION §6).
- `P0.08` — `Tests/Project/`: GameConfig, build list, input maps, localization — 22 edit-mode tests green.

## 3 · Open questions

- Default rounds per case in the lobby (GDD §10).
- Is 12 m the right identify-without-zoom distance? (`docs/LAYOUTS.md`)
- Launch discount; demo / Next Fest timing.

## 4 · Finished phases

—
