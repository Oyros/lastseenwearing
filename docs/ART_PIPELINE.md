# Art Pipeline — Last Seen Wearing

How every model is made, and how to know it is done. What it should look like is
`docs/LOOKDEV.md`; the order things are built in is the **PL** phase of `docs/ROADMAP.md`.

Art sources live outside the repo: `C:\Users\gokha\Desktop\LastSeenWearingArt` (D-004).
On-disk rules and the tool list: `LastSeenWearingArt/_Docs/LSW_ArtRules.md`. References:
`LastSeenWearingArt/_Docs/LSW_RefIndex.md`.

---

## 1 · Why the art is shaped this way

The game is about **describing people** (GDD §01, §04.1). The Watcher sees the crowd through
black-and-white, low-resolution CCTV and describes the fugitive over the radio. Every asset is
judged first by one question: *can someone tell it apart and say it in words, on a bad camera,
from above?*

- Silhouette and light/dark value beat colour, detail and texture.
- Hats, hair shapes, masks, balloons, backpacks and build carry identification. Faces barely do.
- Glasses and half masks are **close-range clues**, seen when a patrol stops someone (D-009),
  so they can stay small.
- The patrol and the dog must be identifiable instantly; nobody else may look like them.
- The fugitive, NPCs and plainclothes are built from the **same parts at the same quality**, or
  the fugitive is spotted by mesh quality.

## 2 · Technical rules

| Rule | Value |
|---|---|
| Units | 1 Blender unit = 1 m, metric, Z up; export converts for Unity |
| Shading | Flat only. No subdivision, no smooth shading, no auto smooth |
| Style | Large flat facets, hard edges; no stitches, buttons, wrinkles, logos, text |
| Geometry | Quads and tris, no n-gons; manifold; no loose verts; no internal faces |
| Transforms | Applied before export. Characters: origin at floor between the feet. Props: floor centre |
| Facing | Characters and dog face −Y in Blender (front view = Numpad 1) |
| Modularity | One armature per family; parts are separate meshes on it; swaps are show/hide, never re-rig |
| Body regions | Head, torso, upper arms, forearms, hands, thighs, shins, feet (L/R) as separate meshes, so regions a garment covers are hidden in game |
| Builds | One topology per sex; `Build_Slim` / `Build_Heavy` shape keys (average = basis). Every torso-worn shell carries the same keys |
| Height | Not modelled: Unity scale 0.92–1.10. Model at average (1.75 m male) |
| Palette | Only colours from `lsw_palette.py` (PL.01). Clothing in light/dark **value pairs** |
| Materials | Flat colour, one atlas per family where possible; variation by slot / vertex colour, never extra meshes |
| Names | `LSW_<Family>_<Part>[_<Variant>][.L/.R]` — e.g. `LSW_Crowd_Top_Raincoat`, `LSW_Crowd_Forearm.L` |
| Contract | Bone, object and socket names are a contract with Unity once exported. Never rename silently: change it here and log a DECISIONS entry |

### Triangle budgets

| Asset | LOD0 | LOD1 | LOD2 |
|---|---|---|---|
| Base body (all regions, incl. hands) | ≤ 2,200 | ≤ 1,000 | ≤ 400 |
| Hand (each) | ≤ 250 | ≤ 80 (fingers merged) | in body |
| Head + hair shell | ≤ 400 | ≤ 200 | ≤ 80 |
| Top / bottom shell | ≤ 600 | ≤ 300 | ≤ 120 |
| Hat / mask / accessory | ≤ 200 | ≤ 80 | hidden |
| Fully dressed crowd character | ≤ 3,500 | ≤ 1,500 | ≤ 600 |
| Patrol (dressed) | ≤ 4,000 | ≤ 1,800 | ≤ 700 |
| Dog (incl. vest) | ≤ 2,500 | ≤ 1,100 | ≤ 450 |
| Stall | ≤ 1,500 | ≤ 600 | — |
| Ferris wheel | ≤ 6,000 | ≤ 2,500 | ≤ 800 |

`lsw_check.py` reads these. LODs are made last (S8), never instead of a clean LOD0.

## 3 · Armature contract (crowd, plainclothes, fugitive, patrol)

Unity Humanoid-compatible. `.L/.R` per Blender convention; the export maps them.

```
Root
└─ Hips
   ├─ Spine ─ Chest ─ Neck ─ Head
   ├─ Shoulder.L ─ UpperArm.L ─ LowerArm.L ─ Hand.L
   │      Hand.L: Thumb1.L ─ Thumb2.L, Index1.L ─ Index2.L, Fingers1.L ─ Fingers2.L
   ├─ (same for .R)
   ├─ UpperLeg.L ─ LowerLeg.L ─ Foot.L ─ Toes.L
   └─ (same for .R)
```

- **Fingers (D-012):** thumb and index have their own 2-bone chains; middle/ring/little share
  one `Fingers` chain — open, fist, grip, point, thumbs-up. The mesh keeps separate fingers with
  clean root loops so full finger bones can be added later without retopology.
- **Sockets** — empties parented to bones, exported as transforms:

| Socket | Parent | Used by |
|---|---|---|
| `SOCKET_Hat` | Head | hats, crown |
| `SOCKET_Face` | Head | masks, glasses |
| `SOCKET_Back` | Chest | backpack |
| `SOCKET_Shoulder.R` | Chest | shoulder bag |
| `SOCKET_Hand.R` / `SOCKET_Hand.L` | Hand | balloon, umbrella, cuffs, props |
| `SOCKET_Radio` | Chest (patrol only) | radio + talk light |
| `SOCKET_Eye` | Head | first-person camera |

The dog has its own armature (PL.36, D-011) in the same naming style. The contract is final once
PL.08 exports it; from then on a change is a DECISIONS entry.

## 4 · Stages every model passes

| Stage | Name | What happens | Output |
|---|---|---|---|
| S1 | Spec | Read this file, the PL task and every listed reference. Write `SPEC_<asset>` as a Text datablock: purpose, readability words, budget, refs, open decisions | Spec text |
| S2 | Ortho setup | `lsw_refload.py`: front/side/back refs as ortho image empties at target height | `_REF` collection |
| S3 | Blockout | Primitives matching proportions, measured against refs front and side | Blockout |
| S4 | Silhouette gate | `lsw_cctv.py`: greyscale 320×180 lineup with the family's existing assets from the CCTV angle (6 m high, 35° down) + front ortho. Distinguishable and describable in a few words | `_Review/<asset>_cctv.png` |
| S5 | Low-poly mesh | Final faceted mesh in budget; loops at deforming joints (≥ 3 at shoulder, elbow, hip, knee; 2 at wrist, ankle, finger roots); n-gon free, manifold, transforms applied, names correct | Mesh |
| S6 | Materials | Palette materials only; value pair for clothing | Materials |
| S7 | Rig & skin | Bind to the shared armature; weights; builds verified; test poses — A-pose, arms up, crouch, sit, walk contact, walk passing, fist, grip | Skinned mesh |
| S8 | LOD | Planar decimate + manual clean-up; LOD1 hands with merged fingers | `_LOD1`, `_LOD2` |
| S9 | Export | `lsw_check.py` all green, then `lsw_export.py` → `_Export/<Family>/` FBX + JSON (sockets, bones, shape keys, materials, hidden regions, LODs). Re-import to confirm | FBX + JSON |
| S10 | Review sheet | Turnaround (front, side, back, ¾) + the S4 lineup → `_Review/<asset>_sheet.png`. Restore the UI | Sheet |

Props skip S7. Accessories skip build shape keys unless they sit on the torso (backpack, bag, scarf).

## 5 · Exit gate — "is it done?"

A PL task is done only when every applicable line holds. Failures go back to the stage that caused them.

- [ ] Matches references front and side within ~2 % at key heights (shoulder, hip, knee, head top).
- [ ] CCTV lineup: distinguishable from every neighbour at 320×180 greyscale; readability words recorded.
- [ ] Tri count within LOD0 budget (`lsw_check.py`).
- [ ] Flat shading, no n-gons, manifold, no loose geometry, transforms applied, origin correct.
- [ ] Names follow §2; contract unchanged or the change logged.
- [ ] Palette-only materials; value pair present where required.
- [ ] (Rigged) Deforms cleanly in all test poses and all three builds; no clipping.
- [ ] (Clothing) Hides the right body regions; listed in the JSON.
- [ ] LODs present and within budget.
- [ ] Export done, re-import clean, JSON correct.
- [ ] Review sheet saved.
- [ ] Blender UI restored; nothing saved unasked.

## 6 · Log

Append to `LastSeenWearingArt/_Docs/LSW_ArtLog.md` after every PL task:

```
## PL.07 — LSW_Crowd_Body_M (2026-10-xx)
Result: DONE | BLOCKED (reason)
Tris: LOD0 2,140 / LOD1 980 / LOD2 390
Readability: "average man" vs slim/heavy distinct at 320×180
Contract changes: none
Saved: yes (asked) | no — in memory only
Files: Crowd/LSW_Crowd.blend, _Export/Crowd/LSW_Crowd_Body_M.fbx, _Review/LSW_Crowd_Body_M_sheet.png
```

Then tick the task in `docs/ROADMAP.md` and update `docs/STATUS.md` (`/done PL.07`).

## 7 · This machine

- Blender work goes through **Blender MCP** in the user's open Blender — never a spawned
  `blender.exe` (headless EEVEE crashes in the NVIDIA driver here).
- Scripts are plain Python in `LastSeenWearingArt/Scripts/`: edit on disk, re-import the Text
  datablock, run. Generators refuse to reset or save when run interactively; the test tools
  (`lsw_selftest`, `lsw_walktest`) work in a temporary scene and delete it.
- **Never save a `.blend` unasked**; say when a change is only in memory.
- After a probe render, restore render settings, visibility and the UI (overlays and panels on,
  nothing hidden or isolated).

## 8 · Into Unity

`_Export/<Family>/*.fbx` + `.json` → `Assets/_Project/Art/Models/<Family>/`. The JSON drives the
importer (sockets to child transforms, hidden regions per garment, LOD groups). Import settings
and the importer itself are P1.03 / PL.04 work; until then nothing art-side is in the project.
