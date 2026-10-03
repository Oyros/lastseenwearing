# Conventions

Formatting is enforced by [`.editorconfig`](../.editorconfig). This file covers the rules a
formatter cannot check.

---

## 1 · The no-hardcode rule

**Any number that a designer might want to change is a config value, not a literal.**

```csharp
// NO — the value is trapped in this file
_stopTimer = 3f;

// NO — the value is trapped in a prefab, and there are 150 NPCs
[SerializeField] private float _stopDuration = 3f;

// YES — one place to tune, one place to look
[SerializeField] private CaptureConfig _config;
...
_stopTimer = _config.StopDuration;
```

**Allowed literals**, and nothing else:

- `0`, `1`, `-1` used structurally (indices, sign flips, empty checks)
- Mathematical constants that are not tuning (`0.5f` for a midpoint, `2` for a diameter)
- Array sizes and buffer capacities that are implementation detail, not feel
- Anything inside a `*Config` ScriptableObject — that is where numbers are supposed to live

If you catch yourself writing a comment like `// tweak this`, it belongs in a config.
Where each value goes: [`DATA.md`](DATA.md).

---

## 2 · The no-literal-string rule

No text a player can read may appear as a literal — not in C#, not typed into a TMP
component in the inspector. Everything goes through a localization key.

```csharp
// NO
_label.text = "Last seen 40 s ago";

// YES
_lastSeen.Arguments = new object[] { ageSeconds };
_label.text = _lastSeen.GetLocalizedString();
```

Log messages, exception text and editor-only tooling are exempt — those are for us.
Keys and procedure: [`LOCALIZATION.md`](LOCALIZATION.md).

---

## 3 · Naming

### C#

| Thing | Style | Example |
|---|---|---|
| Namespace | `LastSeenWearing.<Assembly>.<Area>` | `LastSeenWearing.Core.Capture` |
| Class / struct / enum | `PascalCase` | `CompositeBuilder` |
| Interface | `IPascalCase` | `IStoppable` |
| Method / property / event | `PascalCase` | `SpendCuff` |
| Private field | `_camelCase` | `_currentHeat` |
| Parameter / local | `camelCase` | `heatDelta` |
| Constant | `PascalCase` | `MaxPlayers` |
| Config ScriptableObject | `<Domain>Config` | `CaptureConfig` |

One public type per file. The file is named after the type.

**Use the glossary in [`../CLAUDE.md`](../CLAUDE.md).** The design, the code and the
localization keys must use the same word for the same thing. It is the `Watcher`, never
`Operator`; `Heat`, never `Suspicion` in code; the `Fugitive`, never `Criminal` or `Suspect`.

### Assets

| Kind | Pattern | Example |
|---|---|---|
| Prefab | `PascalCase` | `Patrol.prefab` |
| Config asset | `<Domain>Config` | `CaptureConfig.asset` |
| Scene | `Area_Name` | `Festival_LayoutA.unity` |
| Material | `M_Subject` | `M_CrowdAtlas.mat` |
| Texture | `T_Subject_Type` | `T_CrowdAtlas_Albedo.png` |
| Audio | `SFX_ / MUS_ / VO_` | `SFX_RadioStatic.wav` |
| Model (from Blender) | `LSW_<Family>[_<Part>]` | `LSW_Crowd.fbx` |

---

## 4 · State changes go through Core

Full picture: [`ARCHITECTURE.md`](ARCHITECTURE.md). When writing code that touches game state:

| Situation | Use |
|---|---|
| `Gameplay` or `UI` wants state to change | Call a server-side request that ends in a `Core` method — an intent, never a result |
| `Core` decided something happened | A C# event raised by `Core`; `Gameplay` and `UI` subscribe |
| The round moves to its next phase | `RoundDirector` only (CLAUDE.md rule 5) |
| A client wants to act (stop, arrest, change, target) | `ServerRpc` with the intent; the server validates against `Core` and replies through state |
| Cosmetic feedback (a flash, a sound) | Local to `Gameplay`/`UI`, never written back into `Core` |

Four rules that are not negotiable:

1. **`UI` never computes a rule.** The Watcher's last-seen timer asks `Core`; it does not
   re-derive it. Two copies of a rule drift.
2. **`Core` never reaches into a scene.** No `FindObjectOfType`, no `GameObject` fields. If a rule
   needs a fact from the world, `Gameplay` hands it in.
3. **Randomness in `Core` takes an injected seed/RNG**, so a round can be replayed in a test and
   every client builds the same crowd.
4. **A client never trusts itself.** A client-side check is a prediction for feel; the server
   decides (CLAUDE.md rule 3).

---

## 5 · MonoBehaviour shape

- `[SerializeField] private` over `public` fields, always.
- Cache references in `Awake`. Do not `GetComponent` or `Find` in `Update`/`FixedUpdate`.
- Physics goes in `FixedUpdate`, input and camera in `Update`/`LateUpdate`.
- No singletons for gameplay state. Config and services are handed in from the bootstrap.
- `[RequireComponent]` where a component genuinely cannot function alone.
- `NetworkBehaviour`s keep netcode at the edge: they translate RPCs and `NetworkVariable`s into
  calls on plain classes, they do not hold rules.

---

## 6 · Input

Bindings are listed in [`DATA.md`](DATA.md) §7; this is only how they are reached in code.

- **One action map per mode, and there are two.** `Field` (patrol, plainclothes, dog, fugitive:
  movement, look, interact, role actions) and `Watcher` (camera wall: select feed, pan, zoom,
  rewind, marks, festival controls). One `.inputactions` asset under `Assets/_Project/Settings/`.
  Push-to-talk lives in both. The map is chosen by role at round start.
- **Go through the generated wrapper class**, never `FindAction("Stop")`. *Generate C# Class* is
  on; a renamed action should be a compile error. The class is
  `LastSeenWearing.Gameplay.Player.LastSeenWearingControls`, generated into `Gameplay/Player/`.
- **Latch presses in `Update`, consume them in `FixedUpdate`.** A frame carries two fixed
  steps or none, so polling a press where it is used either drops it or serves it twice.

---

## 7 · Comments

Explain **why**, never **what**. The code already says what.

```csharp
// NO
// Add heat when the player runs
_heat.Add(_config.RunHeatPerSecond * dt);

// YES
// Running is the one thing the crowd notices on its own (GDD §04.4 — no detection meter,
// only heads turning), so it heats every officer's view of you, not just the nearest.
_heat.Add(_config.RunHeatPerSecond * dt);
```

Reference the GDD by `§` when the code exists because of a design decision. That reference
is what stops the next person deleting it as dead weight.
