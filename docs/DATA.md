# Data & Configuration

> **The answer to "where is that value?" is always: a config asset reachable from
> `GameConfig`.** If a number is anywhere else, that is the bug.

---

## 1 · Why not values on prefabs

Prefab-authored values do not survive scale. Past a few dozen prefabs nobody can answer
"which prefab holds the stop duration?", the same concept drifts to different numbers in
different prefabs, and tuning a feel change means hunting through the Project window.

So: **prefabs hold references and identity. Config assets hold numbers.**

---

## 2 · Tuning constant vs. instance data

| | Tuning constant | Instance data |
|---|---|---|
| Example | "How long is a stop?" | "What does the raincoat look like and which tents stock it?" |
| Changes when | Tuning feel or balance | Content is authored |
| Same for every instance? | Yes | No |
| Lives in | A `*Config` ScriptableObject | A data asset, one per thing |
| Folder | `Assets/_Project/Data/Config/` | `Data/Wardrobe/`, `Data/Targets/`, `Data/Layouts/`, `Data/Gaits/` |

| Instance data asset | Class | Holds |
|---|---|---|
| `Data/Wardrobe/<Item>.asset` | `Core/Wardrobe/WardrobeItem` | Id, slot (top, bottom, shoes, hat, face, back, hand, hair), mesh name from the art JSON, body regions it hides, value (light/dark), the words a player would use for it (loc key) |
| `Data/Wardrobe/WardrobeCatalog.asset` | `Core/Wardrobe/WardrobeCatalog` | Every item, by id |
| `Data/Targets/<Target>.asset` | `Core/Targets/TargetDefinition` | Id, type (open, hidden, fixed, social — GDD §04.4), the NPC action that imitates it |
| `Data/Layouts/Layout_<A–D>.asset` | `Core/Layouts/LayoutDefinition` | **In use (P1.17): `Layout_A`.** Generated from the art's layout JSON by `Editor/Import/LayoutImporter` (menu *Layouts › Import Layout A*), never typed: cameras (position, forward, vertical FOV), target spots by kind, tents, exits, crowd areas, bounds. Tent stocks later |
| `Data/Cameras/CctvFilter_<Name>.asset` | `Core/Config/CctvFilterProfile` | One camera's feed look: resolution (default 320×180), contrast, brightness, grain, scan lines, vignette (D-026) — profiles: `Default` 320×180, the good camera; `Worn` 240×135, harder contrast, more grain, scan lines and vignette [PROVISIONAL] (P1.14) |
| ~~`Data/Gaits/GaitCatalog.asset`~~ | — | Not made (D-025): base walks and traits are `BaseWalk`/`WalkTrait` and the art JSON; the odds are in `CrowdConfig` |

**Watch the boundary.** What a garment is (its slot, its words) is instance data. How much heat
a stop adds is a tuning constant. Putting either in the other's place is the mistake this
section exists to prevent.

---

## 3 · The config catalogue

One asset per domain, all in `Assets/_Project/Data/Config/`, all referenced by `GameConfig`.
Values below are the GDD's first numbers; P1 tunes them.

| Asset | Owns | GDD |
|---|---|---|
| `GameConfig` | The root. References every config below. Nothing else | — |
| `LobbyConfig` | **In use (P0.10, P1.09): min/max players (min 3 during P1), default role selection (D-021).** Players 4–5; a 4-player lobby drops the dog (D-007); rounds per case default and range | §03, §06 |
| `RoundConfig` | **In use (P1.10): rounds per case 3, Live 360 s, briefing/result/case-end 10 s, programme 120/240/300 s, last-cuff chase 45 s — all [PROVISIONAL] (D-028); `RoundConfig_QuickTest` is a 40 s test variant, never wired into a scene.** Round length; programme times — concert 2:00, fireworks 4:00 (flare 10 s), closing 5:00; sunset curve; last-cuff chase 45 s | §06 |
| `CrowdConfig` | **In use (P1.01–02): NPC count, route length, waypoint spread, dwell range, walk speed, stride length and run stride length 2.5 m (art data, test-checked — D-022, D-034), gait odds and pace multipliers (D-025), taken-over sync rate, bump distance and duration.** NPC count 100–150; lookalikes per round 2–3; height scale 0.92–1.10 in three bands; reaction radii (running, bump, dog bark); false-positive target-action rate | §04.1, §04.4 |
| `CompositeConfig` | Errors per composite 1–2; reveal order per round (build+height, hair, walk, then the rest — D-008); witness-confidence levels | §04.1 |
| `CaptureConfig` | Heat sources and rates (stop, running nearby, looking at cameras); full-heat threshold; stop 3 s; complaints per lost cuff 3; cuffs per round 3; wrong-arrest time bonus and panic radius | §04.3 |
| `FugitiveConfig` | Target durations — open 2 s, social 4 s, fixed 3 s, hidden 5 s; targets on map 5, needed 3; exits 2–3; fake walk ≤ 20 s; NPC control 10 s, 1/round; camera panel 3 s | §04.4, §07 |
| `WatcherConfig` | **In use (P1.13, P1.17a): feed switch 1.5 s; zoom camera — narrowest view 15° (~3.9×), 1 doubling/s, ¼ doubling per wheel notch, pan ±35° yaw / ±15° pitch at 20°/s (finer when zoomed), 0.1°/px drag [PROVISIONAL].** Later: rewind 30 s; feed switch time; zoom range and pan speed; announcements 1/round; festival-control anger cost | §04.1, §07 |
| `DisguiseConfig` | Tent uses per round 1; masked-NPC cluster radius at the mask stall | §05 |
| `DogConfig` | Scent trail 10 s; bark radius, stamina cost, NPC anger; move speed | §03 |
| `RadioConfig` | **In use (P1.15): 8 kHz µ-law, 100 ms packets, 200 ms prebuffer, 1 s max buffer, volume, radio band-pass 300–3400 Hz; (P1.16) proximity radius 12 m, leak radius 5 m, leak volume 0.5, on-air hold 0.3 s [PROVISIONAL].** Later: noise by distance to the stage | §04.2 |
| `MovementConfig` | **In use (P0.10, P1.11, P1.12): placeholder capsule walk; patrol walk 1.7 / run 4.2; fugitive run speed, acceleration, turn speed, mouse/stick look, interact range. The fugitive's walk speed is `CrowdConfig`'s (D-029).** Walk/run speeds per role (patrol fast, dog slow), acceleration, look sensitivity | §03 |
| `CameraConfig` | **In use (P1.11, P1.12): fugitive third person — distance, pivot height, shoulder, damping, pitch range, FOV; field first person — eye height 1.68, horizontal FOV 90 (D-030), near clip, pitch range; aim range 30 m; third-person collision radius 0.2 m (P1.17).** Per-role eye heights with the dog | §02 |
| `InterrogationConfig` | Scene 10 s; expression choices | §07 |

Adding a domain means adding a row here **and** a field on `GameConfig`. A config that
`GameConfig` cannot reach is invisible and will be forgotten.

`[PROVISIONAL]` numbers in the GDD go into a config exactly like locked ones — the label is
about the design, not about where the value lives.

---

## 4 · How code gets a config

```csharp
public sealed class StopAction : NetworkBehaviour
{
    [SerializeField] private CaptureConfig _config;   // assigned on the prefab

    private void BeginStop() => _remaining = _config.StopDuration;
}
```

- The prefab field is a **reference**, not a number. That is the only thing tuned per-prefab.
- No `Resources.Load`, no static singleton lookups for config. If a system cannot be handed
  its config, it is constructed in the wrong place.
- Config assets are **read-only at runtime**. Never write to one — in the editor a runtime
  write silently persists into the asset file and you will commit a mystery.
- Configs are **identical on every client** because they ship in the build; the host never sends
  them. A config change is a new build.

---

## 5 · Config is not state

Configs say how the game *behaves*; they never hold where a round *is*. Heat, cuffs left,
complaints, targets done, tent stock, the composite and the round timer are runtime state,
server-owned (CLAUDE.md rule 3), never in an asset.

Player settings (volume, sensitivity, push-to-talk key) are the player's, not config: they live in
a settings file under `persistentDataPath`. A config may hold their **bounds and defaults**.

---

## 6 · Adding a new config

1. Add the class in `Scripts/Core/Config/`, `sealed class XConfig : ScriptableObject` with
   `[CreateAssetMenu(menuName = "Last Seen Wearing/Config/X")]`.
2. Group fields with `[Header]`, constrain them with `[Range]`/`[Min]`.
3. Create the asset in `Assets/_Project/Data/Config/`.
4. Add the reference field to `GameConfig`.
5. Add the row to the table in §3 above.

Step 5 is not optional. This table is the map; an unmapped config is a lost config.

---

## 7 · Input bindings

Asset: `Assets/_Project/Settings/LastSeenWearingControls.inputactions`. Two control schemes,
`KeyboardMouse` and `Gamepad`. Code rules: `CONVENTIONS.md` §6. Fixed in P0.06 (D-014).

Hold timings (Arrest, Announcement: 0.5 s) live on the action's `Hold` interaction in the asset —
input feel, not gameplay tuning (D-014).

### `Field` — patrol, plainclothes, dog, fugitive

| Action | Keyboard + mouse | Gamepad | Note |
|---|---|---|---|
| Move | WASD | Left stick | |
| Look | Mouse delta | Right stick | |
| Sprint | Shift | L3 | Running raises heat (§04.3) |
| Interact | E | A | Stop / question / tent / target, by role and context; also the fugitive's fake alarm (at a stall) and camera panel (at the booth) — GDD §07 |
| Arrest | F (hold 0.5 s) | X (hold 0.5 s) | Patrol only |
| RoleAction | Q | Y | Bark (dog), fake walk (fugitive) |
| RoleAction2 | R | B | Sniff (dog), NPC control (fugitive) |
| Signal | Mouse wheel / 1–4 | D-pad | Patrol hand signals; a `Vector2` — 1 up, 2 right, 3 down, 4 left |
| PushToTalk | V | LB | Radio for the Watcher, proximity for the rest |
| Pause | Esc | Start | |

### `Watcher` — the camera wall

| Action | Keyboard + mouse | Gamepad | Note |
|---|---|---|---|
| SelectFeed | 1–8 | D-pad | The pressed control says which feed; a click on a wall tile goes through `Point` + `Mark` |
| SecondMonitor | Shift (held) | RB (held) | Held with SelectFeed: sends the feed to the second monitor (GDD §03) |
| Point | Mouse position | — | Cursor on the camera wall |
| Pan | RMB drag / WASD | Left stick | |
| Zoom | Mouse wheel | Triggers | |
| Rewind | R (held) | X (held) | Live feed missed while held; a plain button read while pressed |
| Mark | LMB | A | Private to the Watcher (§04.2) |
| CameraLamp | L | Y | |
| FestivalControls | Tab | View | Opens the panel |
| Announcement | N (hold 0.5 s) | R3 (hold 0.5 s) | One per round (GDD §07); held so it is never an accident |
| PushToTalk | V | LB | Radio |
| Pause | Esc | Start | |
