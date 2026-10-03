# CLAUDE.md — Last Seen Wearing

Online asymmetric social hide-and-seek for 4–5 players. One player watches black-and-white CCTV
and describes a fugitive over the radio; the others hunt the festival crowd blind; the fugitive
keeps changing what they were last seen wearing. Unity `6000.5.7f1` · URP · Netcode for
GameObjects (listen server over Steam).

**This file is a router, not a manual.** It is loaded every session, so it stays short.
Read the linked doc when the task needs it — do not read them all upfront.

**Session start, always:** read `docs/STATUS.md`, then the current phase in `docs/ROADMAP.md`.
Refer to work by task ID (`P1.07`, `PL.07`) in commits, DECISIONS entries and conversation.

---

## Read this before you touch anything

| Doing this | Read |
|---|---|
| Anything at all, first action of a session | `docs/STATUS.md` — current phase and task ID |
| Picking or finishing a task | `docs/ROADMAP.md` — **only the current phase** (and PL if the art track is in flight); tick `[x]` when *Done when* holds |
| A design question ("should the game do X?") | `docs/GDD.md` — cite the `§` you relied on |
| Adding/moving code, new system, new folder | `docs/ARCHITECTURE.md` |
| Writing C#, naming anything | `docs/CONVENTIONS.md` |
| Any tunable number, any config asset | `docs/DATA.md` |
| Any user-facing text | `docs/LOCALIZATION.md` |
| Any art, rig, animation or Blender work | `docs/ART_PIPELINE.md` — method, stages, exit gate |
| Judging whether something reads or what it costs | `docs/LOOKDEV.md` + `docs/lookdev/*.png` — targets and budgets |
| Level layout, camera placement | `docs/LAYOUTS.md` |
| Branching, committing, merging, scenes | `docs/WORKFLOW.md` |
| "Why was it done this way?" | `docs/DECISIONS.md` |
| Your own task list | `TODO.md` (personal, gitignored — never commit it) |

`docs/GDD.md` is the design truth. **Never restate GDD content in another doc** — link to
the `§`. If a doc and the GDD disagree, the GDD wins and the doc is a bug.

---

## Hard rules

1. **No hardcoded gameplay values.** Every tunable number lives in a ScriptableObject config
   under `Assets/_Project/Data/Config/`, reachable from `GameConfig`. A MonoBehaviour holds a
   config *reference*, never a raw number. → `docs/DATA.md`
2. **No literal user-facing strings.** Everything a player reads goes through a Localization
   key. The game ships in English; `en` is the only v1 locale. → `docs/LOCALIZATION.md`
3. **Server owns truth.** Round state, roles, targets, suspicion, cuffs, tent inventories and
   the composite are server-authoritative. The crowd is **derived from a seed** on every client;
   only NPCs a player has affected are synced (D-005). → `docs/ARCHITECTURE.md`
4. **Assembly boundaries are one-directional.** `Core → nothing`, `Gameplay → Core`,
   `UI → Core, Gameplay`. If you need to break this, the design is wrong, not the asmdef.
5. **One authority for the round.** Only `RoundDirector` advances a round phase. → `docs/ARCHITECTURE.md`
6. **Every new feature must name what it serves** — Recognition, Relay, Capture or Objective
   (GDD §04.1–§04.4). If it serves none of the four, it is backlog.
7. **Record non-obvious decisions** in `docs/DECISIONS.md`: decision, why, cost of reversing.
8. **Never commit** `Library/`, `Temp/`, `Logs/`, `*.csproj`, `*.sln`, or `TODO.md`.
   Always commit `*.meta` alongside the asset it describes. → `docs/WORKFLOW.md`

---

## Glossary — use these words in code, docs and localization keys

| Term | Means |
|---|---|
| **Watcher** | The player at the CCTV wall. Not "operator", not "spotter" |
| **Field team** | Patrol, Plainclothes and Dog together |
| **Patrol** | The uniformed officer. Holds the cuffs |
| **Plainclothes** | The officer NPCs do not avoid. Questions witnesses, checks tents. Cannot arrest |
| **Dog** | The police dog role, first person. Follows scent from left-behind clothes |
| **Fugitive** | The hiding player. Not "criminal", not "suspect" (a suspect is anyone with heat) |
| **Composite** | The witness sketch the Watcher holds. Contains 1–2 errors |
| **Last seen** | The most recent confirmed outfit, with its age |
| **Gait signature** | A character's walk: base clip + up to two traits |
| **Heat** | Suspicion on one character. An arrest needs full heat |
| **Stop** | The 3-second face check |
| **Cuff** | One of three arrests per round. A wrong arrest spends one |
| **Target** | One of five fugitive jobs; three are needed. Open, hidden, fixed, social |
| **Tent** | Changing tent. Public inventory; one use per round |
| **Round** | 5–7 minutes on one layout |
| **Case** | A match: fixed roles, several rounds, a new layout each round |
| **Layout** | One festival arrangement (A Town Square, B Riverside, C Market Alleys, D Park & Stage) |

---

## Working style

- **Read narrowly.** Targeted reads over whole files — read the `§`, not the GDD.
- **Ask before scope.** 2-person project, locked scope. New systems need a rule-6
  justification, not enthusiasm.
- **Docs and code in English.** Conversation with the user is in Turkish.
- **Unity Editor work goes through Unity MCP** when connected — asmdefs, package installs and
  asset creation get correct `.meta` GUIDs that way; hand-written ones risk collisions.
- **Blender work goes through Blender MCP** (the add-on bridge inside the user's **open**
  Blender) — never a spawned `blender.exe`: headless EEVEE crashes in the NVIDIA driver on
  this machine. Art lives outside the repo and outside git
  (`C:\Users\gokha\Desktop\LastSeenWearingArt`, rules in `_Docs/LSW_ArtRules.md`): edit the
  parametric `lsw_*.py` on disk, re-import the Text datablock, run; restore render/visibility
  state and the UI after a probe; **never save a `.blend` unasked**, and say so when a change is
  only in memory.
- **Update `docs/STATUS.md`** when a task starts or finishes, and tick it in `docs/ROADMAP.md`.
- **Never start a later phase's task** unless ROADMAP marks it `parallel`. Phase exits are gates.
- `/next` (in `.claude/commands/`) proposes the next task; `/done <ID>` closes one.
