# ROADMAP — Last Seen Wearing

From scaffold to Steam release. **Read the phase you are in, not the whole file.**

## How to use this file

- Every task has an ID: `P<phase>.<nn>` (art track: `PL.<nn>`). Commits, DECISIONS entries and TODO items reference it.
- A task is done when its **Done when** line is true — not when code exists.
- A phase is done when **every exit criterion** holds. Do not start the next phase's tasks early
  unless the task or phase says `parallel`.
- Tick tasks here (`[x]`). `docs/STATUS.md` names the current phase and the task in flight.
- Design rules live in `docs/GDD.md`; tasks link the `§` and never restate it.
- **No dates.** Budgets are rough and [PROVISIONAL], used only to notice drift.

## Phase map

| Phase | Name | Answers | Budget |
|---|---|---|---|
| P0 | Scaffold | Does the project open, build and connect two players? | ~3 days |
| P1 | Prototype | Is describing and searching fun? | 3–4 weeks |
| PL | Look-dev & art | Does every asset read on a black-and-white 320×180 camera? | *(parallel to P1–P4)* |
| P2 | Roles & core systems | Do all five roles and the suspicion/stop/cuff rules work, in `Core`, tested? | 4 weeks |
| P3 | Round & case | Does a round have a shape, and a case a story? | 3 weeks |
| P4 | Tools | Do the watcher and fugitive tools add choices, not noise? | 3 weeks |
| P5 | Social, Steam page & demo | Is there a replay, a page and a demo that converts? | 3 weeks |
| P6 | Beta & polish | Is it balanced, stable, accessible? | 4 weeks |
| P7 | Release | Is it on sale? | 1 week |
| P8 | Post-launch | Accomplice mode, rival fugitives, more layouts | open |

---

## P0 — Scaffold

**Goal.** The project opens in Unity `6000.5.7f1`, resolves packages, compiles, is in git, and two
instances join one lobby.

| ID | Task | Done when |
|---|---|---|
| [x] P0.01a | Docs, commands, `.gitignore`/`.gitattributes`/`.editorconfig`, `Packages/manifest.json` mirrored from Borrowed Crown / Pane & Panic | Files exist (D-001) |
| [x] P0.01 | `Assets/_Project/` tree and asmdefs `Core`, `Gameplay`, `UI`, `Editor`, `Tests` (Borrowed Crown layout) through Unity MCP | Tree and asmdefs exist with Unity-generated `.meta` files; references one-directional (ARCHITECTURE) |
| [x] P0.02 | Open in Unity 6000.5.7f1 | Packages resolve, console has zero errors, `.meta` files generated |
| [x] P0.03 | `git init`, LFS for `*.fbx *.png *.wav *.exr *.mp4`, first commit | `git status` clean; `Library/` not tracked |
| [x] P0.04 | `GameConfig` ScriptableObject root in `Data/Config/` | One asset every other config hangs from (`docs/DATA.md`) |
| [x] P0.05 | `Bootstrap.unity` and `Sandbox_Empty.unity`; Bootstrap first in build list | Build runs to an empty scene |
| [x] P0.06 | Input Actions asset: `Field` map (patrol, plainclothes, dog, fugitive) and `Watcher` map; keyboard+mouse and gamepad | Both maps load; bindings listed in `docs/DATA.md` |
| [x] P0.07 | Localization settings, `en` locale, tables `UI`, `Roles`, `Festival` | A test key renders through TMP |
| [x] P0.08 | Test runner: one passing edit-mode test in `LastSeenWearing.Tests` | Test runner green |
| [x] P0.09 | Windows build script (Editor menu) | One click produces a runnable `Builds/` exe |
| [x] P0.10 | NGO + Facepunch transport: host / join a Steam lobby (dev app id 480), Multiplayer Play Mode for a second editor player | Two instances in one lobby; each sees the other's capsule move |

**Exit.** Clean open, clean build, green tests, two players connected, first commit pushed.

---

## P1 — Prototype

**Goal.** Answer one question: **is it fun for the watcher to describe and the field to search?**
Greybox, placeholder everything, three players: Watcher, Patrol, Fugitive.

### Scope — IN

Watcher, Patrol, Fugitive · 2 cameras (black-and-white, low-res) · radio + proximity leak ·
composite with 1–2 errors · tent outfit change · 3 targets · arrest with 3 cuffs · one greybox
layout (LastSeenWearingArt greybox, layout A) · box crowd from a seed.

### Scope — OUT

Plainclothes, Dog, suspicion, stops, witnesses, walk system (beyond the P1.06 spike), festival
programme, sunset, case format, every tool in §07, replay, art, audio.

| ID | Task | § | Done when |
|---|---|---|---|
| [x] P1.01 | Seeded crowd: 150 capsule NPCs wander a NavMesh from one seed | §04.1 | Same seed → same paths on two clients |
| [x] P1.02 | Crowd netcode: host sends seed + only player-affected NPCs (D-005) | §04.1 | 7-min round, 2 clients, no visible desync; bandwidth logged |
| [x] P1.03 | Crowd base body + armature + bone contract from the art track | ART_PIPELINE §3 | PL.08–PL.09 done; rig imports as Humanoid |
| [x] P1.04 | Base walk clips on the crowd rig | ART_PIPELINE, `LSW_WalkSystem.md` | PL.11 done; blend tree switches without foot sliding |
| [x] P1.05 | Additive walk layers | `LSW_WalkSystem.md` §2 | PL.12 done; each layer reads at 0.5 weight |
| [x] P1.06 | Gait signature from seed (quantised buckets, max 2 traits) | §05 | 30 NPCs show distinct walks; log prints each signature in words |
| [x] P1.07 | Black-and-white low-res camera filter | §04.1 | Crowd recorded through it; 2 testers find a named walk in under 20 s (`docs/WALKTEST.md`) |
| [x] P1.08 | Week-1 review: crowd model and walk system go / no-go | — | Decision recorded in DECISIONS |
| [x] P1.09 | Lobby roles: Watcher, Patrol, Fugitive assignment | §03 | Three players get their roles; roles are visible to all |
| [x] P1.10 | Round flow: spawn, timer, end screen; one `RoundDirector` authority | §06 | Round starts, ends on timer, restarts |
| [x] P1.11 | Fugitive controller: third person, NPC-speed walk, interact | §03 | Fugitive moves like an NPC by default |
| [x] P1.12 | Patrol controller: first person, run, placeholder FP arms | §03 | Patrol moves and aims |
| [x] P1.13 | Watcher view: 2 monitor feeds, camera list, switch delay | §04.1 | Switching costs the configured delay |
| [x] P1.14 | Camera filters per camera (B/W, low-res) | §04.1 | Two different filters on two cameras |
| [x] P1.15 | Radio channel watcher → field | §04.2 | Patrol hears the watcher anywhere |
| [x] P1.16 | Proximity voice + radio leak; radio light over a talking officer | §04.2 | A fugitive within range hears the radio; the light shows |
| [x] P1.17 | Greybox layout A in Unity from the art greybox export | LAYOUTS.md | PL.04 done; layout walkable, cameras placed as in layout A |
| [x] P1.17a | Zoom cameras: every camera slowly pans and zooms, as far as its profile allows (D-036); the Watcher's `Pan` / `Zoom` act on the active monitor's camera (rates in `WatcherConfig`) | §04.1, LAYOUTS.md | The Watcher picks out a figure in a "far only" zone of layout A on the zoom camera |
| [x] P1.18 | Modular NPC with 3–4 clothing slots on the base body | §05 | PL.13–PL.15 done; crowd dressed from the seed |
| [x] P1.19 | Composite generation: traits + 1–2 errors; fugitive sees which | §04.1 | Watcher and fugitive panels show the right versions |
| [x] P1.20 | Watcher UI: composite panel, last-seen clothing with age timer | §04.1 | Timer counts from the last confirmed sighting |
| [x] P1.21 | Tent outfit change | §05 | Fugitive enters, picks items, exits changed |
| [x] P1.22 | Targets: 3 spots, timed interaction, completion state | §04.4 | Three done → exit opens |
| [x] P1.23 | Exit and fugitive win | §06 | Reaching the open exit ends the round |
| [x] P1.24 | Arrest, 3 cuffs, police win / out-of-cuffs loss | §04.3 | All three outcomes reachable |
| [ ] P1.25 | Greybox animations: target action, arrest, tent enter/exit | — | PL.17 done; clips play |
| [ ] P1.26 | Round results screen | §06 | Winner and reason shown |
| [ ] P1.27 | Playtest with ≥ 5 sessions of people outside the team; record screen + voice | — | Recordings saved; notes in `docs/playtests/P1.md` |

**Exit — pass (all):**
1. Players ask for "one more round" without being prompted.
2. Watchers describe the describing as the fun part, not a chore.
3. Fugitive wins between 35% and 65% of rounds.
4. At least one "they walked right past me" moment per session.

**If it fails:** one more week on the camera filter, composite and radio. Fail twice → stop the
project, **knowingly** (record it in DECISIONS).

---

## PL — Look-dev & art *(parallel to P1–P4)*

**Goal.** Every asset is modelled, rigged and exported through one pipeline and passes the CCTV
readability gate. Method and gate: `docs/ART_PIPELINE.md`. Targets: `docs/LOOKDEV.md`.
Art lives in `C:\Users\gokha\Desktop\LastSeenWearingArt` (D-004); work through Blender MCP in the
open Blender; never save a `.blend` unasked.

### References

| ID | Task | Ref | Done when |
|---|---|---|---|
| [x] PL.00 | All reference sheets saved under `_Ref` with `LSW_Ref_*` names; target frames in `docs/lookdev/` | `LastSeenWearingArt/_Docs/LSW_RefIndex.md` | Every row of the index has a file |

### Track A — pipeline tools (in order)

| ID | Task | Ref | Done when |
|---|---|---|---|
| [x] PL.01 | `lsw_palette.py`: skin, hair, clothing value pairs, uniform, festival, emissive lights, CCTV red; node colour = viewport colour | LOOKDEV §3 | One call creates every `MAT_LSW_*` in a file |
| [x] PL.02 | `lsw_check.py` technical gate | ART_PIPELINE §5 | Flags each failure type (proven by `lsw_selftest.py`) |
| [x] PL.03 | `lsw_cctv.py` readability gate | ART_PIPELINE §4 S4 | Greyscale 320×180 lineup + front ortho written to `_Review/` |
| [x] PL.04 | `lsw_export.py` mirroring `BorrowedCrownArt/Scripts/bc_export.py`: deform-only FBX + JSON (sockets, shape keys, hidden regions, LODs) | ART_PIPELINE §4 S9 | Test asset exports to `_Export/` and re-imports clean |
| [x] PL.05 | `lsw_refload.py` ortho reference loader | ART_PIPELINE §4 S2 | Crowd refs load with feet at 0 and head at 1.75 m |
| [x] PL.06 | Run `lsw_selftest.py` once in this machine's open Blender | — | CHECK, CCTV and REFLOAD print PASSED; result in the art log |

### Track B — crowd base (prototype needs these first)

| ID | Task | Ref | Done when |
|---|---|---|---|
| [x] PL.07 | Male base body (average), body regions split | `Crowd/_Ref` Body + LowPoly M_Average, Hand | Exit gate (ART_PIPELINE §5); seams closed in A-pose |
| [x] PL.08 | Armature + skin, sockets; contract table final | ART_PIPELINE §3 | Exported; the contract is locked (DECISIONS) |
| [x] PL.09 | Male builds as shape keys (`Build_Slim`, `Build_Heavy`); slim exaggerated | `Crowd/_Ref` M_Slim, M_Heavy | Three builds nameable in a CCTV lineup |
| [ ] PL.10 | Run the walk test with 3–5 people; adjust layer strengths in `LSW_WalkSystem.md` | `docs/WALKTEST.md` | Results recorded; strengths updated |
| [x] PL.11 | Walk base clips (Normal, Brisk, Stroll, Heavy) | `LSW_WalkSystem.md` §2 | No foot sliding in a blend test |
| [x] PL.11a | `Idle_Stand` clip on the crowd rig: weight shift, breathing, same hip height as `Walk_Normal` at rest, loopable; exported in the body FBX and listed in its JSON like the walks | `LSW_WalkSystem.md` §2 | Requested by P1.04 (dwelling NPCs freeze mid-stride until it exists); blends from any walk phase without a pop |
| [x] PL.11b | `Run` clip on the crowd rig for the fugitive's sprint: same contact convention as the walks (`contact_L`/`contact_R` markers), its own stride length in the JSON (`speed` × cycle), loopable, in the body FBX | `LSW_WalkSystem.md` §2 | Requested by P1.11 — the sprint plays the walk cycle faster and the team called it comic; blends from `Walk_*` without a foot pop |
| [x] PL.12 | Walk additive layers (Limp L/R, Hunch, Sway, Bounce, ArmSwing ±) | `LSW_WalkSystem.md` §2 | Each readable at 0.5 weight in a 320×180 walking lineup |
| [x] PL.12a | **Spine pitch sign is inverted in `lsw_walk.py`** (found by P1.05): on this rig a negative X rotation on Spine/Chest bends *back*, so `lean` and `hunch` bend the wrong way and Neck/Head compensate the wrong way. Measured on the exported FBX imported **Generic** (raw bones, no Humanoid; front = +Z by the toes): `Walk_Brisk` head 4.5 cm *behind* the hips (meant +9° forward), `Walk_Stroll` 3.8 cm *ahead* (meant −3°), `Add_Hunch` 18 cm *behind*. Flip the X sign for Spine/Chest/Neck/Head in the style lean and the hunch layer; check arm-swing phase against the legs while there | `LSW_WalkSystem.md` §2 | Generic import of the re-export: Brisk head ahead of hips, Stroll behind, Hunch ahead; P1.05 lineup re-rendered |
| [x] PL.12b | **Feet turned in, knees never straight** (Gokhan, 2026-10-03: "everyone walks like they are hurt"): Blender's leg IK bent the knee in the plane of the rest pose's slight outward bow and twisted the whole leg to point the knee forward, so the toes turned ~50° inward in every clip; hips sat 5 cm low so the stance knee never straightened. Analytic two-bone leg solve with no twist, 6° natural toe-out, hip drop 3.5 cm + 1 cm more bob | `LSW_WalkSystem.md` | Toes 5–10° out in every walk, idle and action clip; stance knee straight at passing; planted feet ≤ 0.5 mm off target |
| [x] PL.13 | Head base + 2 hair shells (crew cut, long) | `LSW_Ref_HeadsHair.png` | Shells swap with no gap or clipping |
| [x] PL.14 | Tops: hooded raincoat, t-shirt, hoodie (hood as a thick bunch) | `LSW_Ref_Clothing.png` | No clipping in all builds and poses; hides the right regions |
| [x] PL.15 | Bottoms: trousers, shorts; hat: baseball cap | `LSW_Ref_Clothing.png`, `LSW_Ref_Accessories.png` | Brim readable from the CCTV angle |
| [x] PL.16 | Prototype crowd review: 12 random combinations | LOOKDEV §2 | Every pair describable as different in ≤ 5 words |
| [x] PL.17 | Greybox animations: wallet lift, poster swap, tent enter/exit, arrest pair | — | Clips listed in the export JSON |
| [x] PL.18 | Patrol uniform on the crowd average body; radio on `SOCKET_Radio` with emissive talk light | `Patrol/_Ref` | Identifiable instantly among 12 crowd figures on camera |
| [x] PL.19 | First-person arms (patrol sleeves), separate rig | — | Idle, walk, stop gesture, cuffs frame in a 90° FOV camera |
| [x] PL.19b | Redo the first-person arms' `FP_Idle` and `FP_Walk` poses: in play the hands point up and in like horns from the bottom corners and read wrong (team, P1.12). Keep the rig, the `Camera` eye bone, the contacts (0/16) and the clip names; frame for a **horizontal** 90° FOV at 16:9. Also: the exporter still writes FBX files without the `.fbx` extension (**fixed during PL.29**: `export_rigged` always adds `.fbx`; crowd, patrol and FP FBX re-exported with it) | `_Review/PL.19_FP_*.png`, D-030 | Requested by P1.12 — the team reads the idle and walk arms as natural in play |

### Track C — greybox festival

| ID | Task | Ref | Done when |
|---|---|---|---|
| [x] PL.20 | Greybox kit (16 pieces) on a 1 m grid | `Festival/_Ref` | `lsw_check` 15/15 pass; CCTV props lineup (D-013) |
| [x] PL.21 | Layouts A–D with 5 targets, tents, exits, 4 cameras; coverage maps | `docs/LAYOUTS.md` | Coverage stats and target visibility recorded |
| [x] PL.22 | Export greybox kit and layouts to Unity (art side done 2026-10-03: `_Export/Festival/Layouts/`; open layout A in Unity to close) | — | PL.04 done; layout A opens in Unity at scale |

### Track D — full crowd, dog, festival (v1)

| ID | Task | Ref | Done when |
|---|---|---|---|
| [x] PL.23 | Female base body + builds on the same armature | `Crowd/_Ref` F_* | Binds; walk clips play without retarget artefacts |
| [x] PL.24 | Every garment fitted to the female body | — | Garments × 2 sexes × 3 builds, no clipping |
| [x] PL.25 | Hair set complete (8 options incl. beard/moustache shells) | `LSW_Ref_HeadsHair.png` | 8 options distinct in a CCTV lineup |
| [x] PL.26 | Tops complete: trench (belted), open flannel over tee, puffer (big bands), sweater, vest | `LSW_Ref_Clothing.png` | No two tops confusable at 320×180 |
| [x] PL.27 | Bottoms complete: skirt, jeans (rolled cuffs / wide leg), overalls | `LSW_Ref_Clothing.png` | Bottoms lineup passes |
| [x] PL.28 | Hats complete: straw (wide), beanie (tall), bucket (wider brim), paper crown | `LSW_Ref_Accessories.png` | Hats lineup from the CCTV angle passes |
| [x] PL.29 | Masks (fox, half, cartoon) on `SOCKET_Face`, light/dark — `LSW_Crowd_Mask_Fox/Half/Cartoon_M/_F`, slot `face`, slot 0 = mask value from `mask_for_skin`, slot 1 = the opposite (eyes, mouth, fox nose/ear insides); full masks carry `excludes: [hat, facial]`, the half mask `close_range: true` | `LSW_Ref_Accessories.png` | Full masks read on camera; half mask at stop distance (D-009) |
| [x] PL.30 | Glasses (regular, sun) — close-range clue — `LSW_Crowd_Glasses_Regular/Sun_M/_F`, slot `glasses`, slot 0 = frame value from `mask_for_skin`, slot 1 = LensDark; `excludes: [face]`, `close_range: true` | D-009 | Readable at 2 m eye level |
| [x] PL.31 | Body accessories: scarf, backpack, shoulder bag, umbrella, balloon — `LSW_Crowd_Acc_<Item>_M/_F`, slots `neck` / `back` / `bag` / `hand_r` / `hand_l` (one item per slot), `Fit_Bulky` key over the Puffer, `Add_Grip_L/R` finger curls | `LSW_Ref_Accessories.png` | Backpack and balloon read on camera |
| [x] PL.32 | Face decal for close-ups (neutral, surprise, anger, panic, blink) — `LSW_Crowd_FaceDecal_M/_F` (slot `face_decal`, `lod0_only`), atlas `_Export/Crowd/LSW_FaceAtlas.png` + `.json` (cell UV offsets) | D-010 | Flush on all heads and builds; hidden at LOD1+ |
| [x] PL.33 | Crowd LODs — `_LOD1` / `_LOD2` for every crowd mesh (`Scripts/lsw_lod.py`), same FBX | ART_PIPELINE §2 | 150-character scene within budget at each LOD |
| [x] PL.34 | Full crowd review: 30 random characters (signed off by Gokhan 2026-10-03: `_Review/PL.34_Crowd30_*`, `Scripts/lsw_crowdreview.py`) | LOOKDEV §2 | Signed off by eye |
| [x] PL.35 | Dog mesh (mouth closed, vest separate) — done 2026-10-03: `Scripts/lsw_dog.py`, `LSW_Dog_Body` 1,432 tris (separate lower jaw that opens: tongue, canines, mouth interior) + `LSW_Dog_Vest` 276 tris in the work file (reworked 2026-10-04 to match the reference) (exported with the PL.36 rig) | `Dog/_Ref` | Vest clearly lighter than the body on camera |
| [x] PL.36 | Dog armature + skin + clips (walk, trot, sniff, bark, sit, pant, turn); first-person `SOCKET_Eye` — done 2026-10-04: `Scripts/lsw_dogrig.py`, `_Export/Dog/LSW_Dog.fbx` + `.json` (35 bones, 12 clips, LOD1/LOD2 1,009 / 410 tris) | D-011 | Clips in JSON with contact frames |
| [x] PL.37 | Crowd idles, reactions (head turn, step back, look up, scatter, flinch) — done 2026-10-04: `Scripts/lsw_reactions.py`, 12 clips + phone prop in `LSW_Crowd_Body_M.fbx` | — | Read in the CCTV render |
| [ ] PL.38 | Target actions shared with NPCs; police stop/arrest/signals/questioning; fight pair; chase set; FP clips | — | No foot sliding; listed in JSON |
| [ ] PL.39 | Stall frame + one roof shape per stall type + dressing | `Festival/_Ref` | Four roofs nameable from the CCTV angle |
| [ ] PL.40 | Changing tent, stage, ferris wheel (12–15 m, emissive), lights | `Festival/_Ref` | Wheel visible from every camera of a layout |
| [ ] PL.41 | CCTV pole (big emissive red lamp, `SOCKET_Lamp`, `SOCKET_Cam`), security booth + panel, barrier, PA, gate | `Festival/_Ref` | Lamp reads at 320×180 |
| [ ] PL.42 | Ground kit, town backdrop (dusk palette) | `docs/lookdev/target_frame_mood.png` | — |
| [ ] PL.43 | Layout test: one layout fully dressed with 150 characters, four camera renders at dusk | LOOKDEV §1 | Signed off against the target frames |

---

## P2 — Roles & core systems

**Goal.** All five roles and every capture rule work, live in `Core`, are config-driven and tested.

| ID | Task | § | Done when |
|---|---|---|---|
| [ ] P2.01 | Plainclothes role (cannot arrest) | §03 | Plays a round as plainclothes |
| [ ] P2.02 | Tent inventories public; missing-item view on entry | §05 | Missing items shown to the plainclothes |
| [ ] P2.03 | Witness memory + questioning (short, vague, may contradict) | §04.3 | Tests: only recent sightings produce a description |
| [ ] P2.04 | Dog role: first person, sniff → 10 s trail from tent clothes; bark → NPC step back, stamina, anger | §03, D-011 | Trail leads to the fugitive's path |
| [ ] P2.05 | Suspicion (heat) per character | §04.3 | Tests: each source raises it by its config value |
| [ ] P2.06 | Stop: 3 s face check, lock, complaints → cuff loss; close-range clues visible only here | §04.3, D-009 | Tests cover the complaint rule |
| [ ] P2.07 | Arrest needs full suspicion; wrong arrest → time bonus + NPC scatter | §04.3 | All outcomes tested |
| [ ] P2.08 | Fake-walk key (presets, ≤ 20 s, blend back), networked state | §05 | Snap-back visible on camera |
| [ ] P2.09 | Composite walk text from the gait signature | §05 | Text matches the strongest 1–2 traits |
| [ ] P2.10 | 5 targets / fugitive picks 3; four types; NPC false positives | §04.4 | NPCs perform target actions at configured rates |
| [ ] P2.11 | Crowd head-turn reaction to running and bumping | §04.4 | Visible on camera |
| [ ] P2.12 | 2–3 exits, the open one chosen by the last target | §04.4 | Tested per layout |
| [ ] P2.13 | Disguise layer: hats, hoods, masks; masked-NPC cluster near the mask stall | §05 | Cluster density from config |
| [ ] P2.14 | 4- vs 5-player role rules (4 players: no dog) | §03, D-007 | Both lobby sizes play |

**Exit.** Every rule above has edit-mode tests; a 5-player round plays start to end.

---

## P3 — Round & case

| ID | Task | § | Done when |
|---|---|---|---|
| [ ] P3.01 | Festival programme timeline (concert, fireworks, closing) | §06 | Events fire on config times |
| [ ] P3.02 | Concert: crowd to stage, radio degradation zone | §06 | — |
| [ ] P3.03 | Fireworks: crowd looks up, camera flare 10 s | §06 | — |
| [ ] P3.04 | Closing announcement: crowd to exits | §06 | — |
| [ ] P3.05 | Sunset lighting curve; camera degradation; lamp pools | §06 | Visible over a round |
| [ ] P3.06 | Last-cuff rule: reveal, 45 s chase, corner condition | §06 | Both outcomes reachable |
| [ ] P3.07 | Case format: fixed roles, N rounds, rotation per case | §06 | — |
| [ ] P3.08 | Composite reveal per round (build → hair → walk) | §04.1, D-008 | Tests |
| [ ] P3.09 | Layout swap per round; layouts A–D in Unity | §06, LAYOUTS.md | PL.22 done; four layouts playable |

**Exit.** A full case of 3 rounds plays; testers can name what changed between rounds.

---

## P4 — Tools

| ID | Task | § | Done when |
|---|---|---|---|
| [ ] P4.01 | Watcher rewind 30 s | §07 | Live feed missed while rewinding |
| [ ] P4.02 | Announcement (PA trap) + head-turn response | §07 | Once per round |
| [ ] P4.03 | Camera red lamp | §07 | Field and fugitive both see it |
| [ ] P4.04 | Festival controls: barriers, light direction, door locks, NPC anger | §07 | Overuse produces chaos |
| [ ] P4.05 | Fake alarm (stall fight) | §07 | — |
| [ ] P4.06 | NPC control 10 s, autopilot body | §07 | — |
| [ ] P4.07 | Watcher booth + camera panel | §07 | One camera blinded for the round |
| [ ] P4.08 | Interrogation table: expression pick, NPC random expressions | §07, D-010 | PL.32 done |

**Exit.** Testers use every tool at least once per case, and none is ignored or dominant. Cut list
if the schedule slips: P4.06, P4.07, P5.03.

---

## P5 — Social, Steam page & demo

| ID | Task | § | Done when |
|---|---|---|---|
| [ ] P5.01 | Round replay (20 s, side by side, near-miss highlights) | §08 | — |
| [ ] P5.02 | Case file card | §08 | — |
| [ ] P5.03 | Spectator guess panel | §08 | — |
| [ ] P5.04 | Camera-test onboarding (also the demo build) | §08 | A new player finishes it unaided |
| [ ] P5.05 | Trademark search for the title (USPTO, EUIPO, classes 9 and 41) | — | Result in DECISIONS before the page is public |
| [ ] P5.06 | Steam page: capsule, trailer, text from `docs/STEAM_PAGE.md` | — | Page live |
| [ ] P5.07 | Demo release; Next Fest if timing allows | — | Demo live |

---

## P6 — Beta & polish

| ID | Task | Done when |
|---|---|---|
| [ ] P6.01 | Balance pass from playtest data (win rates per role, per layout) | Every role/layout inside 40–60% |
| [ ] P6.02 | Achievements | — |
| [ ] P6.03 | Settings, controller support, comfort options | — |
| [ ] P6.04 | Release candidate | No blocker bugs |

---

## P7 — Release

| ID | Task | Done when |
|---|---|---|
| [ ] P7.01 | Launch at $9.99 with launch discount | On sale |

---

## P8 — Post-launch

Accomplice / false-witness mode · rival fugitives · more layouts (toward 6–8).
