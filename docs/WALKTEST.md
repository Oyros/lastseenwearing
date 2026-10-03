# Walk readability test — how to run it

**Current files (P1.07, Unity):** `docs/walktest/LSW_WalkTest_near.mp4`, `LSW_WalkTest_far.mp4`, `LSW_WalkTest_key.mp4`.
Rebuild: open `Sandbox_WalkTest`, enter play mode, **Last Seen Wearing → Art → Record Walk Test (in play mode)**
(`Editor/Tools/WalkTestRecorder`). Keys 1 / 2 / 3 switch near / far / key by hand in play mode.

Thirty identical crowd bodies (the real rig and clips, no clothes) walk a 10 × 10 m plaza. They differ **only** in how
they walk. Six carry one trait each at **0.5 weight** — the LOOKDEV §2 gate; everyone else walks plain at one of the
four base walks. The three files show the same 12 seconds: through the CCTV filter (greyscale, 320×180, grain, scan
lines) from a camera 6 m up at **8 m** and at **18 m**, then the raw key. Seed 2026; the log's answer key:
limps left = Walker_10, limps right = Walker_00, hunched = Walker_22, bouncy = Walker_29, stiff arms = Walker_20,
sways = Walker_16 (`WalkTestCrowd`).

*Earlier art-side version (box figures, `lsw_walktest.py`): `LastSeenWearingArt/_Review/walktest/`.*

## Protocol (2 minutes per tester)

1. Show the tester **near** first, without the answer key. Ask one question at a time, they point at the screen (pause allowed):
    - "Find the one limping on the **left**."
    - "Find the one who is **hunched**."
    - "Find the one **bouncing**."
    - "Find the one with **stiff arms**."
    - "Find the one **swaying**."
2. Then the same five questions on **far**.
3. Check answers against **key** (colours: red = limps left, orange = limps right, blue = hunched, green = bouncy, purple = stiff arms, yellow = sways).
4. Note per question: found / wrong person / gave up, and roughly how many seconds.

## Reading the results

| Result | Meaning for the walk system |
| --- | --- |
| Found within ~10 s at 8 m | Trait reads at identification range: keep the strength |
| Found at 8 m but not at 18 m | Expected: walks are a near-range clue, like faces. Fine |
| Not found at 8 m | Trait too subtle: exaggerate that additive layer before authoring final clips |
| Left/right limp confused | Watchers can't tell sides on camera: either make the side obvious (hip drop + lean) or describe limps without a side |
| Wrong person picked often | A base walk (heavy, stroll) looks like a trait: make base walks more alike or the trait stronger |

Current strengths in the test (procedural): limp = 55% stride on the bad leg + 7 cm hip drop + 10° lean; hunch = 31° spine pitch; bounce = 10 cm vertical bob (normal 2.5 cm); stiff arms = arm swing ~2° (normal ~26°); sway = ±11° upper-body roll. Feed the findings back into `LSW_WalkSystem.md` §2 before the Blender agent authors A1.05.
