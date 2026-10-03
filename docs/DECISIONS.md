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

### D-005 — Crowd is derived from a seed
**Decision.** Every client simulates the same 100–150 NPCs from the round seed (appearance, gait,
routes). Only NPCs a player has affected — bumped, stopped, controlled, scattered — become
server-owned and synced.
**Why.** Syncing 150 transforms at 4–5 players costs bandwidth for no gameplay; a seed costs one int.
Gait signatures in particular never need sending.
**Reversing.** Confirmed by P1.02 (D-019). If determinism ever drifts, fall back to server-owned NPCs
with reduced-rate transforms — a networking rewrite of the crowd, not of the rules.

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

---

### D-015 — Addressables build with the player, set per project
**Decision.** `AddressableAssetSettings.BuildAddressablesWithPlayerBuild` is `BuildWithPlayer` in
the committed asset, not left on "use preferences". `WindowsBuild` (P0.09) has no separate
Addressables step.
**Why.** Localization tables ship through Addressables. The default defers to a per-user editor
preference, so one machine's build would carry the text and the other's would not. Verified in
P0.09: a menu build logs `ui.menu.title` as "Last Seen Wearing" from the player.
**Reversing.** Cheap: flip the setting and add a `BuildPlayerContent()` call to `WindowsBuild`.

---

### D-016 — Unity Transport in the editor, Steam in builds
**Decision.** `NetworkSession` picks the transport per instance: Unity Transport on `127.0.0.1`
in the editor (main editor + Multiplayer Play Mode clones), Facepunch/Steam in player builds. In
the editor the session starts itself when `Sandbox_Empty` loads: the main editor hosts, every MPPM
clone joins. The Steam path (friends-only lobby, overlay invite, join by the owner's Steam id) is
the same code with the other transport.
**Why.** One Steam account cannot connect to itself, and every MPPM clone runs as the same account,
so a Steam session cannot be tested on one machine. D-002 still holds for the game; this is only
how a developer gets two players without a second PC.
**Reversing.** Cheap: flip `_editorTransport` to Steam on the bootstrap. The Steam path is unproven
until two accounts run it (P0.10 stays open for that).

---

### D-017 — The crowd plan is ours: own PRNG, per-NPC streams, no local avoidance
**Decision.** `Core/Crowd/SeededRandom` (xorshift64* seeded through SplitMix64) is the only RNG the
crowd uses; NPC `i` draws from `Derive(seed, i)`. `CrowdPlanner` turns the seed into each NPC's
spawn and looping route of waypoint legs (offset, dwell). `CrowdAgent` walks the plan with a
`NavMeshAgent` whose obstacle avoidance is off. The pinned-sequence test fails if the PRNG changes.
**Why.** "Same seed → same crowd" must hold across machines and runtimes; `System.Random`'s
sequence is an implementation detail, and local avoidance depends on frame timing. Per-NPC streams
keep NPC `i`'s plan stable when the crowd size changes. Verified in P1.01: host and MPPM clone build
identical plans and identical NavMesh path corners.
**Reversing.** The PRNG is cheap to swap but re-rolls every seed. Turning avoidance on needs P1.02's
answer on how much positional drift the crowd netcode tolerates.

---

### D-018 — Crowd bone contract locked
**Decision.** The crowd skeleton is `LSW_Crowd_Rig_M` from `LastSeenWearingArt/Scripts/lsw_rig.py`: 34 bones
(ART_PIPELINE §3), `Root` non-deform above `Hips`, Blender `.L/.R` names kept in the FBX, seven sockets as empties
parented to bones. Exported as `_Export/Crowd/LSW_Crowd_Body_M.fbx` + `.json` (PL.08).
**Why.** Unity Humanoid needs a stable bone set before P1.03 builds the avatar, and every garment, hair shell and
clip from here on is skinned against these names. Weights come from a position function, not hand painting, so
regions and future garments get identical weights where they meet and can be regenerated.
**Reversing.** Expensive after P1.03: a renamed or re-parented bone breaks the avatar, every exported garment and
every clip. Adding a bone (e.g. full fingers) is cheap — the mesh keeps separate fingers for that (D-012).

---

### D-019 — An untouched NPC is a function of server time
**Decision.** An NPC's pose is `NpcSchedule.Evaluate(crowd time)`: its plan's legs laid out on the
NavMesh, walked at a fixed speed with dwells, looping (`Core/Crowd`). Crowd time is NGO server time
minus the crowd's start, which the host sends once with the seed. Nothing else is sent for an
untouched NPC. An affected NPC (P1.02: a bump) is taken over by the server; its pose goes to clients
as a named message (index, x, z, heading — 16 B) at `CrowdConfig.TakenOverSyncRate`, and a late
joiner asks for every taken-over NPC once. `NavMeshAgent` is gone from the crowd.
**Why.** A per-frame simulation drifts with frame timing and leaves a late joiner behind. Measured in
P1.02 over 7.4 min, editor host + MPPM client: the client's crowd clock sat a constant 0.049–0.052 s
behind (NGO's 50 ms server buffer) with no growth; the same NPC differed by at most ~7 cm. Crowd sync
was 0 B/min untouched, 80 B for one bump; a client that left and rejoined mid-round rebuilt the same
crowd with the bumped NPC in place.
**Reversing.** NPCs react only once taken over; anything reactive (scatter, stop, NPC control) is a
takeover. Removing the 50 ms offset is one line (add `ServerBufferSec` on clients) if it ever shows.

---

### D-020 — A-pose art, T-pose avatar; the shared Fingers chain drives the middle finger
**Decision.** Rigged `LSW_*` models under `Art/Models/` — "rigged" is the export sidecar's
`"kind": "rigged"` — import as Humanoid through `Editor/Import/HumanoidImportPostprocessor`, ported
from Borrowed Crown (its D-030/D-031): an explicit bone table (`HumanoidBoneMap`, never auto-mapping),
two-pass import, and an avatar skeleton that is `TPoseBuilder`'s T-pose built from the file's A-pose
rest. The mesh and bind pose stay A-pose. D-012's shared `Fingers1/2` chain maps to Unity's middle
finger; ring and little stay unmapped; thumb and index map to their own.
**Why.** The contract (D-018) is fixed, so the map is a table, not a guess. Verified in P1.03: valid
Humanoid, all 33 deform bones mapped, rest-pose muscles all inside ±1 and left/right symmetric,
Humanoid round trip ≤ 0.1 mm at hands, feet, head and finger tips, and muscles bend the right way
(arm up, knee back, fingers into the palm).
**Reversing.** Adding full finger chains later is a table edit plus a reimport; clips made before
then animate only the middle finger for the three shared fingers.

---

### D-021 — Role hand-out is a lobby setting: pick or random
**Decision.** The host chooses how roles are handed out, per lobby: **Pick** — players claim roles,
and whoever has none when the host locks gets a random free one; **Random** — no claims, every role
is drawn when the host locks. The default is `LobbyConfig.DefaultRoleSelection` (Pick). Lock needs
Watcher, Patrol and Fugitive covered; nothing changes while locked. The rules live in
`Core/Roles/RoleRoster`; `Gameplay/Roles/RoleRosterSync` (server-owned, public to all — D-006)
publishes them; `UI/Lobby/RosterPanel` shows them. Roles belong to the case, not the round, so the
roster is not on `RoundDirector`.
**Why.** GDD §03/§06 fix roles per case but not how they are chosen; the team wanted both ways,
chosen per lobby. Random draws use the shared `SeededRandom`, so a logged seed replays a draw.
**Reversing.** Cheap: a third mode (host assigns) is another branch in `RoleRoster`.

---

### D-022 — The walk cycle runs on distance; a rigged body's rest is its bind pose
**Decision.** The crowd animator (`AC_Crowd`, built by `Editor/Import/CrowdAnimatorBuilder` from the
body's clips) has a `Walk` state — a 1D blend tree over the `BaseWalk` clips on `Style`, its time driven
by `Phase` = metres walked (from `NpcSchedule`) ÷ `CrowdConfig.StrideLength`, Foot IK on — and an `Idle`
state (`Idle_Stand`, PL.11a, own clock) entered when the distance stops growing (`Walking`, 0.25 s). Clips play
in place (rotation, height, XZ baked; height from feet). `BodyClipPostprocessor` sets every clip from
the body's sidecar JSON. Each NPC's base walk is `GaitPlanner.BaseWalkFor(seed, index)` on its own
stream. The Humanoid importer reads a skinned bone's rest from the meshes' bind matrices, not the node.
**Why.** Phase from distance makes a planted foot travel back exactly as far as the body travels
forward at any speed, stops the cycle when the NPC stops, and gives every client the same step for
free. Measured in P1.04 over 4 walks, three 50/50 blends and 0.6/0.94/1.3 m/s: planted-foot drift
≤ 8.3 mm without Foot IK, ≤ 3.7 mm with it. The body FBX now carries its clips, and its nodes held a
clip frame (hands 34–43 cm off rest): an avatar built from the nodes was bent; from the bind pose it
is not (Borrowed Crown D-059 was the same trap).
**Reversing.** Time-scaled playback instead of Phase is a controller rebuild plus `CrowdAgent`; it
brings back sliding whenever speed ≠ 0.9375 m/s × stride.

---

### D-023 — URP asset assigned, GPU skinning on
**Decision.** `Assets/_Project/Settings/Rendering/LSW_URP.asset` (+ `LSW_URP_Renderer`) is the default
pipeline and every quality level's, SRP Batcher on; Player › Mesh Deformation is GPU (batched).
Built-in `Default-Material` users (sandbox ground, boxes, player capsule) moved to URP Lit; the body's
imported materials are URP Lit by import. `Tests/Project/RenderingSetupTests` keeps all of it true.
**Why.** P0 installed the URP package but never assigned a pipeline asset, so the project drew with
Built-in and skinned on the CPU. P1.04 measured 150 bodies at ~10 fps (editor + two MPPM clones);
with URP and GPU skinning, three connected players run ~40 fps on the same machine.
**Reversing.** Nothing to reverse; what is left is the crowd's renderer count (15+ skinned meshes per
body) — a per-NPC mesh combine, decided with clothing (P1.18) and the week-1 review (P1.08).
**Note.** Re-saving a network prefab through prefab contents left `PlayerCapsule`'s
`GlobalObjectIdHash` stale on disk and MPPM clones refused the host ("NetworkConfig mismatch") until
it was saved again.

---

### D-024 — Walk traits are additive layers on the walk's phase, set by signed strength
**Decision.** `AC_Crowd` carries one additive layer per `WalkTrait` (Limp, Hunch, Sway, Bounce,
ArmSwing), named after it, its time on the same `Phase` as the base walk. Two-sided traits blend their
two clips on `<Trait>Side` (−1 / +1). `CrowdAgent.SetTrait(trait, signedStrength)` sets the layer weight
to |strength| and the side to its sign (Limp −left / +right, ArmSwing −stiff / +big); weights fade to 0
with the walk → idle blend. `Editor/Tools/WalkLineup` renders the CCTV lineup (LOOKDEV §2: 6 m, 35°,
320×180, grey) into `docs/lookdev/walk_lineup_cctv*.png`.
**Why.** One layer per trait keeps "max two traits" (WalkSystem §3) a matter of which weights are
non-zero, and the shared phase keeps every delta on the step it was authored for. Measured in P1.05:
no trait at full strength moves a planted foot beyond the base walk's 3.2 mm. The lineup found that the
art's spine pitch is inverted (`Walk_Brisk` leans back, `Add_Hunch` bends back — PL.12a), confirmed on
a raw Generic import, so it is the clips and not Unity's muscle-space additive.
**Reversing.** Cheap — layers are generated; a trait added to `WalkTrait` and the builder's clip table.

---

### D-025 — Every walk is unique, drawn in buckets; uniqueness bends the odds
**Decision.** `Core/Crowd/GaitPlanner.SignaturesFor(seed, count, settings)` gives each character a
`GaitSignature`: a base walk, a pace (`Tempo`: slow / mid / fast — speed × `CrowdConfig` multiplier; the
step follows, D-022) and 0–2 traits in buckets (limp ±slight/±strong, hunch/sway/bounce slight/strong,
arm swing ±1). NPC `i` draws on its own gait stream and redraws until its walk is not taken, so walks
are unique (GDD §05) and the same on every client; a smaller crowd is a prefix of a larger one. There are
828 walks (`GaitPlanner.Capacity`); asking for more throws. `CrowdSpawner` logs every walk in words.
No `GaitCatalog` asset: the clips and traits are already `BaseWalk`/`WalkTrait` + the art JSON.
**Why.** Buckets are what a player can say; uniqueness is the design. Its cost, measured in P1.06 over
twenty 150-NPC crowds: only 12 walks have no trait, so a crowd has 12 plain walkers (config asks 25 %),
71 with one trait and 67 with two (config asks 30 %); strong traits 53 % (asks 40 %). A trait like
"limps left" therefore matches more NPCs than the odds suggest — for P1.07's find test and the P1.08
review (more buckets, or height/build joining uniqueness, are the levers).
**Reversing.** Dropping uniqueness restores the configured odds; GDD §05 would change with it.

---

### D-026 — The CCTV look is applied where a feed is shown; recordings come from Unity
**Decision.** A `CctvCamera` renders into its own point-filtered feed at its `CctvFilterProfile`'s size
(instance data, `Data/Cameras/`; default 320×180). The greyscale treatment — contrast, per-frame grain,
scan lines, vignette, all per *feed* pixel — is the UI shader `LSW/UI/CctvFeed`, applied by
`CctvFeedView` where the feed is shown; the same feed can be shown raw. The walk test is
`Sandbox_WalkTest` (`WalkTestCrowd`: 30 bodies, six traits at 0.5, uniqueness off) recorded with
Unity Recorder (`com.unity.recorder`, editor only) into `docs/walktest/`.
**Why.** A per-camera look is what P1.14 needs (two cameras, two filters) and what the Watcher's wall
(P1.13) is made of: feeds in UI. Filtering at display time keeps the answer key and any debug view
honest — the same pixels, unfiltered. Recording in Unity tests the real rig, clips and filter, not
box stand-ins; game time is locked to the video's frame rate, so the files play at real speed.
**Reversing.** Cheap: a full-screen render feature could replace the UI shader if a feed must be
filtered before it is shown; the profile and camera stay.

---

### D-027 — Week-1 review: the crowd model and the walk system are a go
**Decision.** GO for both (P1.08). The crowd stays seed + server time with server takeover (D-005,
D-019); the walk system stays base walks + additive trait layers on a distance-driven phase with
unique bucketed signatures (D-022, D-024, D-025).
**Why.** Against `LSW_WalkSystem.md` §7: 30+ characters with distinct signatures — 150 unique walks
per crowd, logged in words (P1.06); a recording through the B/W low-res filter — `docs/walktest/`
(P1.07); a named walk found through the filter — the team found every asked-for trait (P1.07; timings
and tester details not recorded); no foot sliding when layers combine — every single trait and every
pair at 0.5 and 1.0 stays under 5 mm (base walk 3.2–3.7 mm, P1.04/P1.05/P1.08). The crowd model held a
7-minute run at ≤ 7 cm and 0 B/min untouched, with late join (P1.02).
**Follow-ups, not blockers.** Per-NPC mesh combine for the 15+ body renderers, with clothing (P1.18;
~40 fps with three editors, D-023). Uniqueness skews the trait odds (D-025): judge it in a 150-NPC crowd
with lookalikes (P1.19), where "limps left" will match more than one person.
**Reversing.** A no-go later would mean server-owned NPC transforms (D-005's fallback) or authored
per-character clips — both large; neither is indicated.

---

### D-028 — The round: one director, six minutes, three rounds, time up goes to the police
**Decision.** `Gameplay/Round/RoundDirector` (server) is the only thing that moves the round, through
`Core/Round/RoundCycle` (Lobby → Briefing → Live → [LastCuff] → Result → next round | CaseEnd → Lobby).
It times each phase from `RoundConfig`, raises `Core/Round/Programme` (opening, concert, fireworks,
closing) to every client, spawns each non-Watcher player's body for Live — Patrol, Plainclothes and Dog
at their role's point, the fugitive where an NPC stands — and despawns them at Result. NGO no longer
spawns a player on connect. Every round gets a new crowd: `CrowdSpawner.Reseed(Derive(caseSeed, round))`.
CaseEnd unlocks the roles (they rotate next case). [PROVISIONAL] values: Live 6 min, 3 rounds per case,
and **time up = police win** — the fugitive did not get out before the festival closed.
**Why.** GDD §06 fixes the programme but not the round's length, the case's size or a time-up outcome;
the team chose these in P1.10. Verified with three MPPM players on a 40 s test config: two full rounds,
programme on time, bodies by role, a new crowd seed each round reaching the clients, back to the lobby
with roles unlocked.
**Reversing.** Cheap: values are `RoundConfig`; the time-up winner is one line in `RoundRules`.

---

### D-029 — The fugitive is a crowd body that walks on the crowd's own code
**Decision.** The fugitive (`Prefabs/Characters/Fugitive.prefab`, `Gameplay/Player/FugitiveController`) is
the crowd NPC's body — same model, visible parts and animator — driven by the same `Gameplay/Crowd/
WalkCycle` the NPCs now use. It walks at `CrowdConfig.WalkSpeed` × its pace (only the sprint, turning,
look and interact range are `MovementConfig`'s), its walk is `GaitPlanner.CharacterSignature(crowd seed,
crowd size, slot 0)` — drawn after the crowd's, so no NPC shares it — and the server hands it the
crowd seed right after spawning it. Every client animates it from the distance its body covers. The owner
steers relative to a Cinemachine third-person camera on a look pivot (`CameraConfig`). Interact asks the
server, which checks range and the target (`Gameplay/Interaction`). `RoundDirector` spawns a prefab per
role; Patrol stays a capsule until P1.12.
**Why.** "Moves like an NPC by default" (P1.11) is strongest when there is one walk implementation, not
two that agree. Tested: a fugitive and an NPC given the same walk and distance produce identical animator
phase, walking state and layer weights; played by the team, it blends in.
**Open.** The sprint plays the walk cycle faster and looks comic (team, P1.11): a run clip is asked of the
art track (PL.11b).
**Reversing.** Cheap for the camera and config; the shared `WalkCycle` is the point and should stay.

---

### D-030 — The patrol: first person on the crowd's walk, arms seated at their own eye
**Decision.** The patrol (`Prefabs/Characters/Patrol.prefab`, `Gameplay/Player/PatrolController`) is PL.18's
uniformed body on the crowd contract, walking on the crowd animator through `WalkCycle` (a plain Normal
walk — the patrol is not hiding); the owner sees only its shadow. The owner's camera is first person at
`CameraConfig`'s eye height (1.68 m, the body's `SOCKET_Eye`) with PL.19's arms parented to it. The arms
are seated by their rig's non-deforming `Camera` bone — moved so that bone is the camera — and
`CameraConfig`'s first-person field of view is **horizontal**, as Blender frames it, turned into Unity's
vertical angle for the screen's aspect each frame. The arms' animator (`AC_FPArms`, generated by
`FpArmsAnimatorBuilder`) walks on the body's distance phase. Rigged exports without `Hips` import Generic
(`GenericRigPostprocessor`). Aim is a view ray (`AimProbe`) to the first thing in the way; every character
carries a trigger `CharacterHitbox` on the `Character` layer; `AimTarget` is what P1.24's stop and arrest
act on. Patrol sprint 4.2 m/s beats the fugitive's 3.4 (GDD §03: the patrol is fast).
**Why.** Seating by the eye bone and matching the FOV convention reproduces the framing art signed off in
`_Review/PL.19_FP_*.png`; any other offset is a guess that drifts when art moves the arms. Measured in play:
at Unity's vertical 90° the arms read too big and high; at horizontal 90° they match the review sheets.
**Open.** The team found the arms' idle and walk poses poor (P1.12 play test) — PL.19b asks art to redo them.
**Reversing.** Cheap: the FOV convention is one line; the eye-bone seat is one method.

---

### D-031 — The camera wall: a local switch delay, and only shown cameras render
**Decision.** The Watcher's wall (`UI/Watcher/WatcherWall`) shows two monitors and the camera list on the
Watcher's screen only. `Core/Watcher/FeedSwitcher` decides what each monitor shows: switching to another
camera costs `WatcherConfig.FeedSwitchSeconds` (1.5 s, provisional) of static; the camera already shown or
already coming is free; a new choice mid-switch restarts the wait. This state is the Watcher's own client's —
not on the server-truth list (CLAUDE.md rule 3), never sent. Only cameras on a monitor render; with the wall
up the main camera is off (the Watcher has no body). Four placeholder corner cameras stand in `Sandbox_Crowd`
until layout A's (P1.17).
**Why.** What the Watcher looks at changes no one else's game, so a server round trip would only add lag.
Rendering only the shown feeds keeps the CCTV cost at two cameras however many a layout has.
**Reversing.** Cheap: if switching ever needs to be seen by others (a "camera moved" tell for the fugitive),
send the selection by RPC and keep `FeedSwitcher` as is.

---

### D-032 — Voice: our own packets, routed by the server, 8 kHz µ-law
**Decision.** Voice is not Steam voice (ARCHITECTURE said so before). The talking client records the
microphone (`Gameplay/Voice/VoiceCapture`), resamples it to `RadioConfig`'s rate (8 kHz) and µ-law encodes
it (`Core/Voice/MuLaw`, one byte a sample, 100 ms packets ≈ 8 KB/s). Packets go to the server as NGO named
messages; the server checks the speaker may use the channel and forwards each packet only to the clients who
hear it (`Core/Voice/VoiceRouting`, roles from the roster). Listeners play each speaker through a jitter
buffer (`Core/Voice/JitterBuffer`) on a streaming clip (`VoicePlayback`). The radio is one way — Watcher
talks, field team hears it anywhere (GDD §03: field "listen only") — played flat through a 300–3400 Hz
band-pass. The fugitive never receives radio packets; the leak (P1.16) will be a proximity rule.
**Why.** One path that works over both transports (Unity Transport in the editor, Steam in builds — D-016),
so it can be built and tested on one machine; Steam voice cannot run in MPPM. Server routing makes "who
hears what" server truth (CLAUDE.md rule 3) — a client never gets audio its role must not hear. 8 kHz is
already a radio's band, so the codec's limit is the look. Verified in play: 88 packets talked, 88 reached
the patrol and were heard, the fugitive got none.
**Cost.** Bandwidth: the host relays each talker to every listener (radio: ≤ 3 × 8 KB/s). Proximity voice
may want a better rate; Opus (or Steam's codec) can replace µ-law behind `VoiceCapture`/`VoicePlayback`
without touching routing.
**Reversing.** Moderate: the codec is swappable; the routing should stay.

---

### D-033 — Proximity, the radio leak and the radio light
**Decision.** Every body talks in proximity (Field PushToTalk): the server forwards it to every body within
`RadioConfig.ProximityRadius` (12 m), played 3D from the speaker. The radio leaks: for each radio packet the
server sends anyone with a body who is not on the field team — the fugitive — a `RadioLeak` copy if they are
within `LeakRadius` (5 m) of an officer, played 3D from the *nearest* officer, quieter, through the radio
band-pass. The radio light (`RadioLight` on PL.18's lamp) reads GDD §04.2's "talking officer" as *the officer's
radio talking*: every officer's light is lit while the Watcher is on air (server-held, 0.3 s hold, broadcast to
all), so the fugitive learns "the Watcher is describing someone now". The field team cannot talk on the radio,
so this was the consistent reading; the alternative (lit while the officer talks in proximity) was offered and
not taken.
**Why.** Who hears what stays server truth (D-032): a fugitive far from every officer receives nothing, so no
client can cheat the leak. Positions come from the player bodies the server already has.
**Open.** Plainclothes will carry a radio too: a lit radio would expose them — a design question for when
Plainclothes arrives. Stage noise on the radio waits for the layout.
**Reversing.** Cheap: radii and volumes are config; the light's trigger is one call.

---

### D-034 — The run shares the walk's phase; speed is averaged, not read off a frame
**Decision.** The crowd animator's `Walk` state is a 1D blend on `Run` (0 = the base walks, 1 = PL.11b's
`Run` clip), still timed by `Phase`. Both cycles put contact_L at 0 and contact_R at 0.5, so one phase drives
both. `WalkCycle` sets `Run` from the body's speed between its walk speed and its run speed
(`SetRunSpeeds`: fugitive = its pace / `MovementConfig.FugitiveRunSpeed`, patrol = its walk / run) and
stretches the stride from `CrowdConfig.StrideLength` (1 m) to `RunStrideLength` (2.5 m, art data: JSON speed
× cycle) with the blend, keeping the phase continuous. Every client derives `Run` from what it sees, so
nothing is sent. Walk traits fade out as the body runs: a limp is a walk's, and running hides it — at the cost
of being noticed (GDD §04.3). The crowd never runs: for it the phase is exactly D-022's walked / stride.
`WalkCycle` averages speed over 0.1 s and counts a body as walking while it moved within 0.1 s; a single
frame's step is capped at 10 m/s.
**Why.** Played: the sprint read as out of step. Measured: the owner moves on 50 Hz physics, so per-frame
speed swung between 0.56× and 1.1× (and to zero at 144 fps — standing on 65% of frames), so `Run` and
`Walking` flickered. Averaged, a 50 Hz body at 144 fps never reads standing and reaches the full run
(test); the run's planted foot moves back 2.5 m per cycle, matching the stride.
**Reversing.** Cheap: traits during the run are one factor; the smoothing window is one constant.
