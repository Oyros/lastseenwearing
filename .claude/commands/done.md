---
description: Close a roadmap task — verify, tick, update status
argument-hint: <task ID, e.g. P1.07>
---

Close task `$ARGUMENTS`.

1. Find it in `docs/ROADMAP.md` and read its *Done when* line.
2. Verify it holds: run the relevant tests / check the files. If it does not hold, say what is
   missing and stop.
3. Tick it `[x]` in `docs/ROADMAP.md`.
4. Update `docs/STATUS.md` §1 (in flight / next) and §2.
5. If a non-obvious decision was made, add a `docs/DECISIONS.md` entry citing the task ID.
6. If this was the last task of the phase, check every exit criterion of the phase and report
   which hold and which do not. Do not move the phase forward unless all hold.
7. Suggest a commit message: `<ID>: <short summary>`.
