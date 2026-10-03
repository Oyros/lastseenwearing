# Decisions — Last Seen Wearing

One short entry each: decision, why, cost of reversing.

---

### D-001 — Mirror the Borrowed Crown / Pane & Panic project layout
**Decision.** Same folder shape, asmdef boundaries, `.editorconfig`, `.gitattributes`, doc set,
`/next` and `/done` commands as Borrowed Crown; package manifest from Pane & Panic.
**Why.** The layout is proven on two live projects and the same agent workflow drives all three.
**Reversing.** Cheap now, expensive after the first hundred files.

---

### D-002 — Netcode for GameObjects + Facepunch transport, listen server over Steam
**Decision.** Keep Pane & Panic's netcode stack: NGO 2.13.2, Facepunch Steamworks transport,
one player hosts. App id 480 (Spacewar) until the store page exists.
**Why.** Online 4–5 players is the game (GDD §02). The stack already ships in P&P; lobbies, invites
and voice come with Steam.
**Reversing.** Expensive — every stateful system has an ownership model built on it.

---

### D-003 — Cinemachine pinned to 3.1.7
**Decision.** `com.unity.cinemachine` 3.1.7 in the manifest.
**Why.** Fugitive third-person camera and the Watcher's camera rigs. 3.1.4 does not compile on
Unity 6000.5 (`GetInstanceID` obsolete-as-error); 3.1.7 does (Borrowed Crown D-005).
**Reversing.** Do not downgrade below 3.1.7 while on 6000.5.

---

### D-004 — Art lives outside the repo
**Decision.** Blender sources, references, review renders and scripts live at
`C:\Users\gokha\Desktop\LastSeenWearingArt`. Only exported `.fbx` / `.json` / `.png` enter `Assets/`.
**Why.** Same rule as Borrowed Crown and P&P: `.blend` files are not mergeable and bloat the repo.
**Reversing.** Cheap.

---

### D-005 — Crowd is derived from a seed [PROVISIONAL]
**Decision.** Every client simulates the same 100–150 NPCs from the round seed (appearance, gait,
routes). Only NPCs a player has affected — bumped, stopped, controlled, scattered — become
server-owned and synced.
**Why.** Syncing 150 transforms at 4–5 players costs bandwidth for no gameplay; a seed costs one int.
Gait signatures in particular never need sending.
**Reversing.** P1.02 decides with a spike. If determinism drifts, fall back to server-owned NPCs with
reduced-rate transforms — a networking rewrite of the crowd, not of the rules.

---

### D-006 — Roles are public
**Decision.** Everyone knows who plays which role (GDD §03 [LOCKED]).
**Why.** The game's tension is the information split, not hidden identity. A hidden-officer dilemma
does not work when roles are known, so it was cut (GDD §09).
**Reversing.** Expensive — the whole role set would need redesigning.

---

### D-007 — A 4-player lobby drops the dog
**Decision.** 5 players: Watcher, Patrol, Plainclothes, Dog, Fugitive. 4 players: the dog is not in.
**Why.** Watcher, Patrol and Fugitive are the core loop; Plainclothes adds the tent and witness
information. The dog is the most additive role.
**Reversing.** Cheap — a lobby rule.

---

### D-008 — The composite reveals per round
**Decision.** Over a case the Watcher's composite gains one trait per round: round 1 height and
build, round 2 hair, round 3 walk, later rounds the rest (GDD §04.1).
**Why.** Gives a case a shape and makes later rounds sharper without new mechanics.
**Reversing.** Cheap — a list in `CompositeConfig`.

---

### D-009 — Glasses and half masks are close-range clues
**Decision.** They are not modelled to read on the CCTV; they read at stop distance (2 m, eye level).
**Why.** Makes the stop action carry information (GDD §04.1) and keeps those accessories small in
budget and silhouette.
**Reversing.** Cheap in art (bigger meshes); a design change in what a stop is worth.

---

### D-010 — Faces are a decal, close-ups only
**Decision.** A P&P-style face decal (eyes + mouth atlas, UV offset) shown only at LOD0 / close
cameras: interrogation table and stop. Atlas cells: neutral, surprise, anger, panic, blink.
**Why.** Faces don't read on CCTV; expressions are needed only up close (GDD §07).
**Reversing.** Cheap until PL.32.

---

### D-011 — The dog player plays in first person
**Decision.** The dog role uses a first-person camera on `SOCKET_Eye`; the scent trail is an overlay.
**Why.** Matches the rest of the field team (GDD §02) and keeps the dog's model off the player's own
screen.
**Reversing.** Cheap — a camera rig.

---

### D-012 — Shared finger chain
**Decision.** Thumb and index have their own 2-bone chains; middle, ring and little share one
`Fingers` chain (ART_PIPELINE §3). Mesh keeps separate fingers with root loops.
**Why.** Covers open, fist, grip, point and thumbs-up at a third of the bones; full fingers can be
added later without retopology.
**Reversing.** Cheap until PL.08 locks the contract; after that a contract change.

---

### D-013 — Greybox built headless in the cloud; on this machine generators run in the open Blender
**Decision.** The greybox kit, layouts A–D, coverage maps and walk test (PL.20, PL.21) were built and
rendered with a headless Blender 5.2 in a cloud workspace. On this machine every `lsw_*.py` runs in
the user's open Blender through Blender MCP: generators refuse to factory-reset or save when
interactive, and test tools work in a temporary scene they delete.
**Why.** Work continued while the PC was off; headless EEVEE crashes in the NVIDIA driver here, so
the local path must never spawn `blender.exe`.
**Reversing.** Nothing to reverse; re-running a generator locally rebuilds the same file in memory.

---

### D-014 — Input gaps closed in the asset, hold timings stay in it
**Decision.** P0.06 adds three bindings the draft lacked: `Announcement` (N / R3, hold 0.5 s),
`SecondMonitor` (Shift / RB, held with `SelectFeed`) and `Point` (mouse position). The fugitive's
fake alarm and camera panel are not actions; they fire from `Interact` at the stall or booth.
Hold durations (Arrest, Announcement) live on the `Hold` interaction in the `.inputactions` asset,
not in a config.
**Why.** GDD §03/§07 name the tools but the draft table had no key for them. LB is already the
Watcher's push-to-talk, so the second-monitor modifier moved to RB and the announcement to R3.
Hold length is input feel, edited in the Input Actions editor; a config would split one binding
across two assets.
**Reversing.** Cheap: rebinding is an asset edit; moving holds to a config means a processor read.
