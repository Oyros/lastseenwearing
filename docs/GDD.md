# LAST SEEN WEARING — Game Design Document

> **Somewhere in this crowd is the person you're looking for. You just can't see them.**
>
> One player watches the CCTV and describes the fugitive over the radio. The rest hunt the
> crowd blind. The fugitive keeps changing what they were last seen wearing.

---

| | |
|---|---|
| **Genre** | Online asymmetric social hide-and-seek / party game |
| **Players** | 4–5 online (Watcher, Patrol, Plainclothes, Dog, Fugitive; 4-player lobbies drop the Dog) |
| **Platform** | PC · Steam |
| **Engine** | Unity `6000.5.7f1` · URP · Netcode for GameObjects (listen server over Steam) |
| **Round** | 5–7 minutes; a match is a case of N rounds (lobby setting) |
| **Price** | $9.99 |
| **Team** | 2 people (Game Label) |
| **Document status** | Living. Version 1. Written from the design sessions of 2026-10-02. |

---

## About this document

This GDD sets **direction** and the rules the code must follow. Numbers will move in playtest;
the shape of the systems will not move casually.

| Marker | Meaning |
|---|---|
| **[LOCKED]** | Defines the game. Changing it is a redesign; say so and state the cost. |
| **[PROVISIONAL]** | Current answer, expected to move after playtests. |

Locked: the information split (§01 core rule), roles are public (§03), voice-only relay (§04.2),
the stale description loop (§05), three cuffs per round (§04.3). Everything with a number is
[PROVISIONAL] until the P1 gate.

---

## §01 The Game

One player watches the security cameras and describes the fugitive over the radio. Their teammates on the ground can only tell who is who through that description. Meanwhile the fugitive changes clothes and makes the description stale.

**Core rule [LOCKED]:** the player who can see cannot move; the players who can move cannot tell people apart. Information travels only by voice, and it degrades.

- Tone: tension + comedy. Lore is atmosphere only.
- Audience: friend groups and streamers.
- Price: $9.99.
- Setting: an evening festival (stalls, costume/mask tents, stage, fireworks), sliding into sunset during each round.

## §02 Scope & Constraints

| Constraint | Decision |
| --- | --- |
| Team | Gokhan (modelling, rigging, animation), Lukas (engine, netcode); both write code |
| Timeline | 3-6 months, parallel to Pane & Panic and Borrowed Crown |
| Players | Online, 4-5 per lobby |
| Cameras | Watcher: monitor UI. Field team: first-person. Fugitive: third-person |
| Visual style | Stylised low-poly; separated from P&P by palette and light (sunset + warm lamps vs P&P's daytime cartoon palette) |
| Off-limits | Overlap with P&P or Borrowed Crown; heavy writing/lore |

## §03 Roles

Roles are public: everyone knows who plays what. [LOCKED]

| Role | Can | Cannot |
| --- | --- | --- |
| Watcher (1) | Watch cameras (2 monitors), rewind 30 s, talk on radio, festival controls, camera lamp, 1 announcement/round | Move, arrest, place markers visible to others |
| Patrol (1) | Fast; holds the cuffs; stop and arrest | Talk on radio (listen only); hand signals allowed |
| Plainclothes (1) | Not avoided by NPCs; question witnesses; enter tents and see missing inventory | Arrest |
| Dog (1) | Sniff left-behind clothing → 10 s trail; bark to startle nearby NPCs | Fast movement; each bark costs stamina + NPC anger |
| Fugitive (1) | Blend in, change clothes, do targets, fake walk (≤20 s), control one NPC for 10 s/round, fake alarm, cut a camera at the panel | Change height, build or true walk |

In a 4-player lobby the dog drops (watcher, patrol, plainclothes, fugitive). The dog player plays in first person.

## §04 Core Mechanics

### §04.1 Recognition (watcher finds the right person)
- Inputs: composite sketch (permanent traits, witness confidence per trait) and last-seen clothing (with age timer).
- Camera types: black-and-white, low-resolution, slow pan + zoom. Switching cameras costs time.
- Crowd generator spawns 2-3 partial lookalikes of the composite on purpose.
- Rewind: last 30 s, live feed is missed while rewinding.
- Composite errors: 1-2 traits of the fugitive's real face are wrong on the composite. The fugitive sees which; the watcher does not.
- Composite reveals over a case: round 1 shows height + build, round 2 adds hair, round 3 adds walk (later rounds add the remaining traits).
- Close-range clues: glasses and half masks are too small for the cameras. They are seen only when a field officer stops someone (face check), which gives the stop action information value.

### §04.2 Relay (watcher describes it to the field)
- Voice only. Watcher marks are private to the watcher's screen.
- Watcher map uses camera numbers; field map uses landmarks. The watcher must translate.
- Camera angles flip left/right relative to the field.
- Festival noise degrades the radio (worst near the stage).
- Radio leaks into proximity voice: a nearby fugitive can hear it. A radio light shows over a talking officer.
- Camera red lamp: anchors a location for the field; the fugitive sees it too.

### §04.3 Capture (field reaches and arrests)
- Suspicion (heat) per character: raised by stops, running nearby, looking at cameras. Arrest requires full suspicion.
- Stop: 3 s face check, officer locked in place. Wrong stops add complaints; 3 complaints = 1 cuff lost.
- Arrest: wrong arrest = cuff lost + fugitive gains time + nearby NPCs panic and scatter.
- 3 cuffs per round.
- Witness questioning (plainclothes): NPCs who recently saw the fugitive give a short, vague, possibly contradicting description.

### §04.4 Objective (fugitive finishes targets unnoticed)
- 5 targets on the map, the fugitive picks 3. Police see all 5 locations, not which are real.

| Target type | Example | Duration | Risk profile |
| --- | --- | --- | --- |
| Open | Lift a wallet | 2 s | Short, in a crowd |
| Hidden | Pick a safe | 5 s | Long, single-camera alley |
| Fixed | Swap a poster | 3 s | Known spot, main square |
| Social | Talk to a vendor | 4 s | Looks like normal NPC behaviour |

- NPCs perform the same actions randomly (false positives).
- No detection meter. Only players' eyes give the fugitive away.
- Crowd reacts to running and bumping (heads turn), readable on camera.
- After 3 targets, one of 2-3 exits opens (decided by the last target).

## §05 Disguise & Traces

| Layer | Traits | Hidden by | Cost |
| --- | --- | --- | --- |
| Permanent | Height, build, walk | Fake-walk key (≤20 s), walking slowly, carrying something heavy | No interactions while faking; snapping back is visible |
| Coverable | Hair, face | Hat, hood, mask, glasses, fake beard | "Masked" becomes a trait; safe only near masked NPC clusters |
| Changeable | Top, bottom, shoes, accessory | Change in a tent | Old clothes stay behind, inventory drops; one use per tent per round |

- Every character (NPCs included) has a unique walk; it appears on the composite from round 3 of a case.
- Tent inventories are public at round start. A missing item tells the police what to look for; tents hold common items so it narrows but never identifies.
- The dog can follow the trail from clothes left in a tent.

## §06 Round & Match

- **Match = case.** Roles fixed for the case; the case is N rounds (lobby setting); each round on a new layout. Roles rotate next case.
- Each round the watcher gains one more composite clue (round 1 height, round 2 hair, round 3 walk...).
- **Layouts:** one festival map, 6-8 handcrafted layouts (stalls, tents, camera angles). 4 at launch. No procedural generation.
- **Round length:** ~5-7 min, fixed programme:

| Time | Event | Effect |
| --- | --- | --- |
| 0:00 | Opening | Daylight, sharpest cameras |
| 2:00 | Concert | Crowd flows to stage, radio breaks up near it |
| 4:00 | Fireworks | Everyone looks up, cameras flare 10 s |
| 5:00 | Closing announcement | Crowd walks to exits |

- **Sunset:** light fades over the round; cameras degrade; festival lamps create readable pools of light.
- **Win:** fugitive finishes 3 targets + exits → fugitive. Fugitive arrested → police. All cuffs spent → last-cuff rule: identity revealed, no arrests, 45 s chase; exit → fugitive, cornered → police.

## §07 Tools

| Tool | Owner | Effect | Cost |
| --- | --- | --- | --- |
| Rewind | Watcher | Replay last 30 s | Misses live feed |
| Festival controls | Watcher | Barriers, stage light direction, door locks | NPC anger; overuse = chaos that covers the fugitive |
| Announcement | Watcher, 1/round | PA trap ("blue jacket, your child is at the info desk") | Single use |
| Camera lamp | Watcher | Shows field which area is watched | Fugitive sees it |
| NPC control | Fugitive, 1/round | Possess an NPC 10 s; own body on autopilot | Autopilot walk unchanged |
| Fake alarm | Fugitive | Start a fight at a stall | Pulls police and cameras |
| Camera panel | Fugitive | Blind one camera for the round (3 s at the booth panel) | Booth camera sees approach; NPC technicians also visit |
| Interrogation table | Arrest | 10 s scene: fugitive picks an expression; wrong pick reveals. Faces use a P&P-style decal (eyes + mouth atlas) shown only in close-ups | Innocent NPCs pick randomly |

## §08 Social & Onboarding

- Round replay (20 s): watcher camera + fugitive path side by side, near-misses highlighted.
- Case file card per player at match end. No progression.
- Spectator guess panel (in-game / Twitch). Does not affect play.
- Onboarding: 2-minute "camera test" (one camera, one composite, click the right person). Also the demo.
- Marketing look: police radio + CCTV aesthetics, not missing-person posters.

## §09 Not in v1

- After launch: accomplice / false-witness mode; rival fugitives mode.
- Cut: plainclothes dilemma, fugitive fake questioning, giving old clothes to NPCs, fugitive mirror, NPCs remembering wrong arrests, two-step package target, thermal camera.

## §10 Open Questions

- [ ] Default rounds per case.
- [ ] Launch discount.
- [ ] Crowd netcode model (see `ROADMAP.md` P1.02).
- [ ] Demo / Next Fest timing.
- [ ] Trademark search for the title (USPTO/EUIPO, classes 9 and 41).
