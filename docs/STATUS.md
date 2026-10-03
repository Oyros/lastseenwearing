# Status

**Read this first in any new session.** Where the project is, what is in flight, what is
blocking. Shared and committed — personal task lists go in `TODO.md`.

Keep it short. A snapshot, not a log; finished phases move to §4 as one line.

---

## 1 · Right now

| | |
|---|---|
| **Phase** | **P1 — Prototype** (`docs/ROADMAP.md`) — is it fun for the watcher to describe and the field to search? |
| **In flight** | Nothing |
| **Next** | `P1.09` lobby roles, then `P1.10` round flow — `P1.04`–`P1.07` wait on the walk clips (PL.10–PL.12) (`/next`) |
| **Blocking** | Nothing — UnityMCP connected; Blender MCP timed out this session (only matters for PL work) |
| **Parallel** | **PL — Look-dev & art**: `PL.00`–`PL.09`, `PL.20`, `PL.21` done (references, palette — 85 `MAT_LSW_*` + `_Export/LSW_Palette.json`, check/CCTV/ref-loader tools, `lsw_export` FBX+JSON bridge — greybox kit pieces already in `_Export/Festival/`, layouts A–D, male base body — 15 regions, 1,722 tris, `Scripts/lsw_body.py`; rig `LSW_Crowd_Rig_M` — 34 bones, 7 sockets, contract locked D-018, `_Export/Crowd/LSW_Crowd_Body_M.fbx` with `Build_Slim` / `Build_Heavy` blend shapes). **P1.03 is unblocked** (PL.08–PL.09 done). Next art: `PL.10` walk test with people, then `PL.11` walk base clips |
| **Unity** | 6000.5.7f1 · URP · Netcode for GameObjects + Facepunch transport (listen server over Steam) |

## 2 · Done in this phase

- `P1.01` — `Core/Crowd` (own PRNG, `CrowdPlanner`, D-017), `CrowdConfig`, `Sandbox_Crowd.unity` (40×40 m, 16 waypoints, baked NavMesh) now the dev sandbox; 150 NPCs; host and MPPM clone get the same seed, plans and path corners.
- `P1.02` — NPC pose = `f(seed, server time)` (`NpcSchedule`, D-019); bumped NPCs taken over and synced (16 B/NPC/update); 7.4 min run: constant 50 ms client offset, ≤ 7 cm, 0 B/min untouched; late join rebuilds the same crowd. `CrowdDebugPanel`: bump button + 30 s drift probe log.
- `P1.03` — `LSW_Crowd_Body_M` in `Art/Models/Crowd/` imports as Humanoid via `Editor/Import` (bone table, A-pose → T-pose avatar, ported from Borrowed Crown, D-020); 8 tests: contract match, sockets, T-pose, muscle range + symmetry, round trip ≤ 0.1 mm, build blend shapes.

## 3 · Open questions

- Default rounds per case in the lobby (GDD §10).
- Is 12 m the right identify-without-zoom distance? (`docs/LAYOUTS.md`)
- Launch discount; demo / Next Fest timing.

## 4 · Finished phases

- **P0 — Scaffold** (2026-10-03): tree + asmdefs, git/LFS on GitHub, `GameConfig`, Bootstrap → Sandbox, input maps (D-014), localization, 25 edit-mode tests, one-click Windows build (D-015), listen-server session — editor + MPPM on Unity Transport (D-016) and two Steam accounts in one lobby, each seeing the other's capsule move.
