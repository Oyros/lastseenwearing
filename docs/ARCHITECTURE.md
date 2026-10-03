# Architecture — Last Seen Wearing

## Assemblies

One-directional, no exceptions. Breaking one means the design is wrong, not the asmdef.

```
Core      -> nothing (pure C#, no MonoBehaviour, no scene access, no netcode)
Gameplay  -> Core, Netcode for GameObjects, Facepunch transport
UI        -> Core, Gameplay
Editor    -> Core, Gameplay, UI   (Editor platform only)
Tests     -> all                  (Editor platform only)
```

`Core` holds rules that can be unit-tested with no engine: composite generation and its
errors, gait signatures from a seed, heat, the stop/complaint/cuff arithmetic, target and
exit selection, tent inventories, the round programme, case progression. If it can be proven
with a number, it lives here.

`Gameplay` owns MonoBehaviours, NetworkBehaviours, scenes, physics, input, voice and the round
state machine. It reads `Core` and drives it; it never re-implements a rule.

## Round state machine

Single authority: `Gameplay/Round/RoundDirector` on the server. One enum (`Core/Round/RoundPhase`),
one transition table (`Core/Round/RoundCycle`).

```
Lobby -> Briefing(roles, composite) -> Live(programme: opening, concert, fireworks, closing)
   -> [LastCuff chase] -> Result(replay) -> next round of the case | CaseEnd
```

Nothing outside `RoundDirector` advances the round; clients read the phase from a
`NetworkVariable` and subscribe to its change. Festival events (concert, fireworks, closing)
are scheduled by `Core/Round/Programme` and raised by the director.

## Networking

Listen server over Steam (Facepunch transport), same model as Pane & Panic. In the editor the
same session runs on Unity Transport so Multiplayer Play Mode clones can join (D-016).
`Gameplay/Network/NetworkSession` on the bootstrap object owns starting and ending a session.

| Thing | Authority | Synced how |
|---|---|---|
| Roles | Server | `RoleRosterSync` (`NetworkList`), public to all — the case's, not the round's (D-021) |
| Round phase, timer | Server | `NetworkVariable`s on `RoundDirector` |
| Crowd (100–150 NPCs) | **Seed + server time** | Every client computes each NPC's pose from the seed and server time (`NpcSchedule`); only NPCs a player has affected (bumped, stopped, controlled, scattered) are taken over by the server and their poses sent (D-005, D-019) |
| Gait signatures | Seed | `f(seed, index)` computed locally; never sent |
| Player characters | Owner moves, server validates | `NetworkTransform` (owner authority) |
| Fugitive outfit, fake walk | Server | Small state (outfit ids, preset id, on/off) |
| Composite, tents, targets, heat, cuffs | Server | Server-only state; clients get what their role may see |
| Watcher camera feeds | Local render | Each client renders the cameras it is allowed to see; nothing streamed |
| Voice | Steam voice | Radio channel (watcher → field) + proximity; the leak is a proximity rule on the radio stream |

**Information hiding is part of the design.** The server never sends a client data its role
must not know (the fugitive's identity to police clients, the composite's errors to the
watcher). Role-filtered messages, not hidden UI.

## UI

`UI` renders `Core` state and sends requests; it computes no rule (CONVENTIONS §4). The watcher
screen asks `Core` for the composite and the last-seen age; it does not derive them.

## Randomness

One seeded `System.Random` per round on the server, logged at start; the crowd seed is shared
with clients. Rules in `Core` take an injected RNG so a round can be replayed in a test.
