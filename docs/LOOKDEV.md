# Look-dev — Last Seen Wearing

The look is proven on **target frames** before content is built at scale. Every asset after
that is judged by two questions: does it sit in the target frame, and does it still read on the
Watcher's camera?

Design intent (style, palette separation from Pane & Panic) is GDD §01–§02. This file is the
technical target that makes it true. Method is `docs/ART_PIPELINE.md`.

---

## 1 · Target frames

Images in `docs/lookdev/`. Each is a target for **some** things and explicitly **not** for others.

### Field / mood — `target_frame_mood.png`

| | |
|---|---|
| **Place** | Festival square at dusk: stalls, string lights, ferris wheel, crowd |
| **Target for** | Palette and light: warm lamp pools and string lights against cool blue-hour shadow; low-poly faceted style; crowd density |
| **Not a target for** | Camera (the field team plays first person); exact stall roofs (one shape per stall type, PL.39) |

### Watcher — `target_frame_watcher.png`

| | |
|---|---|
| **Place** | The Watcher's monitor: four greyscale CCTV feeds, timestamps, camera numbers |
| **Target for** | CCTV treatment: greyscale, low resolution, scan noise, timestamp overlay; the red camera lamp as the one colour accent |
| **Not a target for** | Final UI layout (P2/P5 UI work); any text in the image |

### Palette — `palette_from_mood.png`

Seed values for `lsw_palette.py` (PL.01). Lamp ambers, string-light colours, dusk blues, skin
and hair ranges. Clothing is chosen in **value pairs** (§3).

## 2 · CCTV readability — the gate that matters most

The Watcher's feed is what makes or breaks the game. Every asset family is tested in it.

| | Target |
|---|---|
| Test render | Greyscale, **320×180**, camera 6 m high, 35° down (`lsw_cctv.py`) |
| Character pass | Any two different crowd characters describable as different in **≤ 5 words** |
| Build | Slim / average / heavy nameable ("thin", "normal", "big") |
| Patrol | Identifiable instantly among 12 crowd figures |
| Dog vest | Clearly lighter than the body |
| CCTV lamp | Reads as a lit point at 320×180 |
| Walk | A gait trait reads at 0.5 weight in a walking lineup (`docs/WALKTEST.md`) |
| Close-range clues | Glasses and half masks need **not** read on camera; they must read at stop distance — 2 m, eye level (D-009) |
| Distance | Identify without zoom up to ~12 m (open question, `docs/LAYOUTS.md`) |

`docs/lookdev/greybox_props_cctv.png` and the layout coverage maps are the first evidence;
`layout_coverage_stats.json` holds the numbers.

## 3 · Colour and value

- **Value before hue.** Two cameras are black and white (GDD §04.1); colour is weak evidence.
  Every hue in the clothing palette has a light and a dark entry.
- Masks contrast with skin in value.
- Patrol uniform: dark base with a **light** hi-vis vest and cap band — the only character that
  carries both extremes.
- Emissive: festival lamps and string lights (warm), ferris wheel, CCTV red lamp, radio talk
  light. Nothing else glows.
- Separation from Pane & Panic comes from palette and light (dusk + warm lamps against P&P's
  daytime cartoon palette), not geometry style (GDD §02).

## 4 · Light over a round

The round slides into sunset (GDD §06). Light is a mechanic, not decoration.

| Time | Light | Cameras |
|---|---|---|
| 0:00 Opening | Daylight, low sun | Sharpest |
| 2:00 Concert | Golden hour; stage lights on | Slight noise |
| 4:00 Fireworks | Blue hour; flare for 10 s | Flare, then noisier |
| 5:00 Closing | Dusk; lamp pools readable, between them dark | Noisiest; lamp pools are where people can still be told apart |

Exposure rule: dusk must stay **readable** in the field. Do not go darker than the mood frame.

## 5 · Performance budgets

**Minimum spec: GTX 1660 (6 GB), 60 fps at 1080p** on the field camera with 150 characters.

| | Budget |
|---|---|
| Characters on screen | 100–150, dressed budgets in ART_PIPELINE §2 |
| Watcher cameras | 4 feeds rendered at 320×180 each (cheap by design); full-res only for the selected feed |
| Materials | One atlas per family; GPU instancing / SRP batcher friendly; colour by property |
| Animation | Crowd walks are base clip + additive layers from the gait seed; LOD2 animates at reduced rate |
| Lights | Lamp pools are baked/light-probe where static; ≤ 8 realtime lights near the camera |

Numbers are targets until P1 measures them (P1 exit gate). Quality tiers come with P6.
