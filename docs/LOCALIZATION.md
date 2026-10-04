# Localization

v1 ships **English only**. The system is built in from day one anyway, because retrofitting
localization means auditing every string in the game — and because the HUD composes strings at
runtime (last-seen age, item names in the tent inventory, announcement text).

**Package:** `com.unity.localization` 1.5.12 (pulls in Addressables).

---

## 1 · The rule

**No text a player can read is ever a literal.** Not in C#, not typed into a TMP component
in the inspector, not in a prefab — and not painted into a texture: posters, stall signs and the
CCTV overlay carry no words in their art; every label is live text over them.

Exempt, because they are for us and never shipped to a player: `Debug.Log` messages,
exception text, editor tooling, gizmo labels.

---

## 2 · Key scheme

```
domain.context.item
```

All lowercase, `snake_case` segments, no spaces. Use the glossary words (`CLAUDE.md`) — a key
says `watcher`, `fugitive`, `heat`, never `operator`, `criminal`, `suspicion`.

| Key | Where it appears |
|---|---|
| `ui.menu.title` | Main menu title |
| `ui.hud.cuffs` | Patrol HUD cuffs label |
| `ui.watcher.last_seen` | "Last seen {0} s ago" (Smart) |
| `role.watcher.name` | A role's name |
| `role.patrol.hint.1` | A role's one-line onboarding hint |
| `wardrobe.raincoat.name` | A garment's word — what the Watcher would say |
| `festival.announcement.child_at_desk` | A PA announcement template (Smart) |
| `festival.event.fireworks` | Programme event banner |

**Domains map to string tables:**

| Table | Domain |
|---|---|
| `UI` | Menus, HUD, settings, lobby, results, replay, case file card |
| `Roles` | Role names, onboarding hints, role-action prompts |
| `Festival` | Garment and accessory words, stall and landmark names, announcements, programme events |

Tables live in `Assets/_Project/Localization/Tables/`.

**Landmark names are content, not decoration.** The field team navigates by landmarks and the
Watcher by camera numbers (GDD §04.2); the landmark words on the field map and the Watcher's map
legend must come from the same keys.

---

## 3 · Smart Strings

Smart String is a **per-entry flag**. Tick **Smart** on any entry that composes anything — a
count, a garment word, a time.

```
festival.announcement.child_at_desk = "Would the person in the {item}, your child is waiting at the info desk."
```

Do **not** build player-facing sentences by concatenating strings in C#.

---

## 4 · Adding a string

1. Open **Window → Asset Management → Localization Tables**.
2. Pick the table for the domain (§2). If the domain is new, add a table and a row above.
3. Add the key following `domain.context.item`. Fill the English value. Tick **Smart** if it composes.
4. In code, reference it with a `LocalizedString` field — never a hardcoded key string:

```csharp
[SerializeField] private LocalizedString _cuffsLabel;
...
_label.text = _cuffsLabel.GetLocalizedString();
```

For UI text components, use `LocalizeStringEvent` instead of setting `.text` by hand.

---

## 5 · Adding a language later

A data task, not an engineering task:

1. **Window → Localization Settings → Locale Generator**, add the locale.
2. Export the tables (CSV or XLIFF), translate, import.
3. Check overflow — several languages run 30–40 % longer than English. UI expands, never clips.

Voice is the players' own; there is no VO to translate.

---

## 6 · Current state

Set up in P0.07:

```
Localization/LocalizationSettings.asset      (registered as the active settings)
Localization/Locales/en.asset
Localization/Tables/UI, Roles, Festival      (collection + shared data + _en table each)
Assets/AddressableAssetsData/                (created by the package; table content ships through it)
Assets/TextMesh Pro/                         (TMP Essential Resources)
```

Keys so far: `ui.menu.title`; `ui.lobby.*` (roster panel, P1.09 — `player`, `player_you`, `row` are Smart);
`role.<watcher|patrol|plainclothes|dog|fugitive>.name`; `ui.round.*` (P1.10); `ui.watcher.camera_n` (Smart),
`ui.watcher.switching` (P1.13), `ui.watcher.camera_zoom` (Smart, P1.17a); `composite.*` (P1.19 — `line` and `wrong` are Smart); `lastseen.*`, `ui.watcher.marked` (P1.20 — `clothes`, `garment`, `when` are Smart); `tent.*` (P1.21 — `row` and `keep` are Smart, `tent.refused.<reason>` per refusal); `objective.*` (P1.22 — `count`, `done`, `exit_open`, `marker` are Smart; `objective.kind.<kind>`, `objective.exit.<exit name>`). `Festival`: `festival.event.*` (P1.10), `wardrobe.<id>.name` for every garment and hair style (P1.18); `person.<sex|height|build|skin|haircolour|tone>.*` and `gait.*` walk words (P1.19–P1.20).

Tables load **asynchronously**: a `LocalizeStringEvent` fills its text a frame or two after start,
so nothing may read a localized string synchronously on the first frame. Player builds must build
Addressables content first — the build script's job (P0.09, WORKFLOW §8).

- Startup locale: `SpecificLocaleSelector` = `en`.
- Fonts: TMP Essential Resources as placeholder; a CCTV/terminal-style face for the Watcher
  overlay is P5 art work.
- Capitals are a **style**, never a string.
