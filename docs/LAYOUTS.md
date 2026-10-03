# Last Seen Wearing — Greybox Layouts A-D

Four festival layouts built from the greybox kit (`LastSeenWearingArt/Festival/LSW_Festival_Greybox.blend`, collections `_WIP_Layout_A`..`_D`, rebuilt by `LastSeenWearingArt/Scripts/lsw_layouts.py`). Each has 5 targets of the 4 GDD types, 2-3 changing tents, 2-3 exits and 4 CCTV cameras on 5.7 m poles. Renders and coverage maps: `LastSeenWearingArt/_Review/layout_<id>_*`.

Coverage is measured by `lsw_coverage.py`: every 1 m cell of open ground is tested for a clear line of sight from each camera to chest height (crowd ignored). **Near** = seen within 12 m (identifiable on the wide camera), **far only** = seen but beyond 12 m (zoom needed to identify), **blind** = no camera sees it.

| Layout | Character | Walkable cells | Near | Far only | Blind | Seen by 2+ |
| --- | --- | --- | --- | --- | --- | --- |
| A Town Square | Balanced, compact 24×24 m square + one alley | 579 | 40.6% | 50.6% | 8.8% | 40.4% |
| B Riverside Promenade | Linear 48×12 m strip, long sightlines, crowd flows end to end | 700 | 38.7% | 38.0% | 23.3% | 32.0% |
| C Market Alleys | Blocks and lanes, corners everywhere, small plaza | 1,120 | 23.3% | 65.9% | 10.8% | 22.8% |
| D Park & Stage | Open 40×32 m park, stage queue, ferris wheel | 1,189 | 21.4% | 63.2% | 15.4% | 20.7% |

## Target visibility (at the spot where the fugitive stands)

| Layout | Open (wallet) | Fixed (poster) | Social (vendor) | Social 2 (vendor) | Hidden (safe) |
| --- | --- | --- | --- | --- | --- |
| A | near, 2+ cams | far | near, 2+ cams | near, 2+ cams | blind |
| B | far | near | near, 2+ cams | near, 2+ cams | blind |
| C | far | far | far | far | blind |
| D | far | far | far | far | blind |

Every hidden target sits in a blind spot and every other target is seen by at least one camera, which is the intended risk spread: hidden = slow but unseen, open/fixed = fast but watched.

## What the layouts say about the game

1. **Two different watcher jobs.** A and B are *identification* layouts: ~40% of the ground is close enough to a camera to recognise someone on the wide view. C and D are *tracking* layouts: about two thirds of the ground is only seen from far away, so the watcher mostly spots movement and must zoom or rely on the field team to confirm. Mixing both kinds across a case keeps the watcher's role fresh.
2. **The zoom camera is essential, not a bonus.** In C and D most targets are only seen "far". Without zoom the watcher cannot tell anyone apart there. The prototype should include one zoomable camera from day one.
3. **Blind spots are the fugitive's map.** B has the most blind ground (23%, mainly the west end behind the stage and the edges of the river row). That makes B the best fugitive layout; A the best police layout. Good for balancing across a case: the layout order can lean toward the side that is losing.
4. **Camera placement rules learned while building:**
    - Keep ~6 m clear in front of each camera; a tent or stall near the pole wipes out a large part of the view.
    - Landmarks block cameras: the ferris wheel hid most of one camera in D until it was moved.
    - In C a corner camera aimed diagonally hit a building wall; in alley layouts cameras must look *along* lanes.
5. **Landmarks read well from every camera** (ferris wheel, stage, green exit gates). They give the watcher vocabulary: "between the wheel and the gate".

## Open level-design questions

- [ ] Is 12 m the right "identifiable without zoom" distance? Test with real character models in the CCTV filter (art roadmap A2.08).
- [ ] Target balance in C and D: all non-hidden targets are "far". Consider moving one target per layout into a near zone so the fugitive faces at least one high-risk choice.
- [ ] Should the case pick layouts in a fixed order or by balance (losing side gets the friendlier layout)?
