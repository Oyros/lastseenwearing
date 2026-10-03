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
| **Next** | Decide the rendering fix below, then `P1.05` additive walk layers or `P1.10` round flow (`/next`) |
| **Blocking** | **Crowd frame rate: ~10 fps with 150 bodies.** Measured in P1.04 (editor + 2 MPPM clones): renderers off 152 fps, animators off still 10 fps — rendering. Causes: **no URP pipeline asset is assigned** (the project renders with Built-in; a P0 gap), CPU skinning, 15 skinned meshes per body (2,250 renderers, 578k verts). `LSW_WalkSystem.md` §6 left this to the week-1 spike (P1.08). |
| **Parallel** | **PL — Look-dev & art**: `PL.00`–`PL.09`, `PL.11`, `PL.11a`, `PL.12`–`PL.15`, `PL.20`, `PL.21` done (references, palette — 85 `MAT_LSW_*` + `_Export/LSW_Palette.json`, check/CCTV/ref-loader tools, `lsw_export` FBX+JSON bridge — greybox kit pieces already in `_Export/Festival/`, layouts A–D, male base body — 15 regions, 1,722 tris, `Scripts/lsw_body.py`; rig `LSW_Crowd_Rig_M` — 34 bones, 7 sockets, contract locked D-018, `_Export/Crowd/LSW_Crowd_Body_M.fbx` with `Build_Slim` / `Build_Heavy` blend shapes). Walk clips `Walk_Normal/Brisk/Stroll/Heavy` in the same FBX (32 f @ 30 fps, stride 1.0 m, in place — Unity moves the body at 0.9375 m/s × playback speed; contacts L 0 / R 16 as markers in the JSON). **P1.04 is unblocked.** Additive layers `Add_Limp_L/R`, `Add_Hunch`, `Add_Sway`, `Add_Bounce`, `Add_ArmSwing_Big/Stiff` in the same FBX (loop 0–32, additive reference = rest pose at frame 40 — see `LastSeenWearingArt/_Docs/LSW_WalkSystem.md` §6). **P1.05 is unblocked.** Head reworked (16-sided, face plane, nose block, eye band, ears; body now 1,872 tris) + hair shells `LSW_Crowd_Hair_Crew_M` / `LSW_Crowd_Hair_Long_M` in the same FBX (two-sided material, `lsw_double_sided` in the JSON; one shown at a time). Tops `LSW_Crowd_Top_TShirt_M` / `_Hoodie_M` / `_Raincoat_M` (hood up) in the same FBX, each with `hides` in the JSON; body gained a `Pelvis` region (crotch → lower belly) so tops hide `Torso` and bottoms will hide `Pelvis`. `Idle_Stand` in the same FBX (120 f @ 30 fps, loops, feet planted, hips at Walk_Normal's mean height −5 cm, speed 0) — P1.04's dwelling NPCs can use it. Bottoms `LSW_Crowd_Bottom_Trousers_M` / `_Shorts_M` and `LSW_Crowd_Hat_Cap_M` in the same FBX — the body now has a full prototype wardrobe (2 hair × 3 tops × 2 bottoms × cap/none; arms grow out of the torso since PL.14b). **P1.18 (modular NPC) is unblocked.** Next art: `PL.16` crowd review (12 combos); `PL.10` (walk test with 3–5 people) is Gokhan's and only retunes layer strengths |
| **Unity** | 6000.5.7f1 · URP · Netcode for GameObjects + Facepunch transport (listen server over Steam) |

## 2 · Done in this phase

- `P1.01` — `Core/Crowd` (own PRNG, `CrowdPlanner`, D-017), `CrowdConfig`, `Sandbox_Crowd.unity` (40×40 m, 16 waypoints, baked NavMesh) now the dev sandbox; 150 NPCs; host and MPPM clone get the same seed, plans and path corners.
- `P1.02` — NPC pose = `f(seed, server time)` (`NpcSchedule`, D-019); bumped NPCs taken over and synced (16 B/NPC/update); 7.4 min run: constant 50 ms client offset, ≤ 7 cm, 0 B/min untouched; late join rebuilds the same crowd. `CrowdDebugPanel`: bump button + 30 s drift probe log.
- `P1.03` — `LSW_Crowd_Body_M` in `Art/Models/Crowd/` imports as Humanoid via `Editor/Import` (bone table, A-pose → T-pose avatar, ported from Borrowed Crown, D-020); 8 tests: contract match, sockets, T-pose, muscle range + symmetry, round trip ≤ 0.1 mm, build blend shapes.
- `P1.09` — roles: `Core/Roles` (`RoleRoster`, pick or random per lobby, D-021), `RoleRosterSync`, localized `RosterPanel`; verified with 3 MPPM players — same roster on all three, conflicting claim refused, random lock, non-host unlock refused.
- `P1.04` — body clips imported from the export JSON (`BodyClipPostprocessor`), `AC_Crowd` built from them (`CrowdAnimatorBuilder`), walk phase from distance + Foot IK (D-022): planted-foot drift ≤ 3.7 mm over 4 walks, blends and 3 speeds (test-checked < 5 mm); importer reads rest from the bind pose. 150 NPCs walk the body, base walk from seed, and idle (`Idle_Stand`) while lingering; garments hidden until P1.18 (prefab shows the `body` slot + crew cut).

## 3 · Open questions

- Default rounds per case in the lobby (GDD §10).
- Is 12 m the right identify-without-zoom distance? (`docs/LAYOUTS.md`)
- Launch discount; demo / Next Fest timing.

## 4 · Finished phases

- **P0 — Scaffold** (2026-10-03): tree + asmdefs, git/LFS on GitHub, `GameConfig`, Bootstrap → Sandbox, input maps (D-014), localization, 25 edit-mode tests, one-click Windows build (D-015), listen-server session — editor + MPPM on Unity Transport (D-016) and two Steam accounts in one lobby, each seeing the other's capsule move.
