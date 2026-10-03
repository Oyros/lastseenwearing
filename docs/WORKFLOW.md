# Workflow

Git, Unity and two people working on the same scenes. Most of this file exists to stop one
specific disaster: **silently corrupting a scene or prefab in a merge.**

---

## 1 · First-time machine setup

1. Install Unity **6000.5.7f1** — the exact version. A different patch version rewrites
   `ProjectSettings/` and produces noisy diffs on every commit.
2. Install [Git LFS](https://git-lfs.com/), then once per machine:
   ```
   git lfs install
   ```
3. Clone the repo and open the root folder in Unity Hub.
4. Configure UnityYAMLMerge (§4). **Do this before your first merge, not during one.**
5. Open the project once. `TODO.md` is created for you automatically.

---

## 2 · Branching

Two shared branches, plus work branches when a change earns one.

| Branch | Role |
|---|---|
| `main` | **Release only.** Every commit on it is a version we would hand to someone. Merged into from `development` and from nowhere else, and tagged when it is |
| `development` | **The working branch.** Where you are by default and where you commit. Keep it buildable |
| `feature/*` `fix/*` `docs/*` | **Optional.** Opened off `development` when a change is big, risky or long-running. While you are on one, you work on it — then merge it back into `development` |

```
development ──●──●──●──────────────●──►(version milestone)──► main ──► tag v0.2
                     └── feature/composite-reveal ──┘
```

- **Default to `development`.** Committing straight to it is normal and expected. This is a
  two-person project, not a queue of pull requests.
- **Open a branch when the change deserves one:** it will take more than a day, it touches
  an assembly boundary or `Core` state (`ARCHITECTURE.md`), or it might not work out.
  `feature/composite-reveal`, `fix/cuff-double-spend`, `docs/architecture-pass`. If you are on
  such a branch, stay on it until the work is done — do not drift back to `development`
  mid-change.
- Branch off `development`, never off `main`. `main` lags behind on purpose.
- Merge a work branch back into `development` yourself when it is finished. A pull request is
  welcome when the other person should see the diff first, but it is not required.
- Merge `development` into your branch often. Long-lived Unity branches produce scene
  conflicts that are not worth resolving.
- A `development → main` merge is a deliberate act, not a routine one. It happens when the
  build is worth a version number, and it carries a tag.

**None of this is enforced.** Branch protection and rulesets are unavailable on a private
repo on the GitHub Free plan — GitHub accepts a ruleset and then does not apply it. `main` is
protected by the habit of not pushing to it and by nothing else.

### Commits

Present tense, one concern per commit:

```
arrest: spend a cuff only on a wrong arrest (P2.xx)
docs: add config catalogue to DATA.md
```

Reference the task ID (`P1.07`) and, when the commit implements a design decision, the GDD
section: `(GDD §04)`.

---

## 3 · What is and is not committed

| Committed | Never committed |
|---|---|
| `Assets/` including every `*.meta` | `Library/`, `Temp/`, `Logs/`, `obj/` |
| `Packages/manifest.json` + `packages-lock.json` | `*.csproj`, `*.sln`, `.vs/`, `.idea/` |
| `ProjectSettings/` | `UserSettings/` |
| `docs/`, `CLAUDE.md`, `README.md` | `TODO.md` (personal — see §7) |

**`.meta` files are not noise.** A `.meta` holds the GUID every reference in the project
points at. Committing an asset without its `.meta`, or deleting one by hand, breaks
references for everyone else. Always stage them together.

---

## 4 · Scenes and prefabs — the dangerous part

Unity's `.unity` and `.prefab` files are YAML. Git's line-based merge does not understand
them and will produce a file that opens without complaint and is quietly wrong.

### Configure UnityYAMLMerge (once per machine)

```
git config --global merge.tool unityyamlmerge
git config --global mergetool.unityyamlmerge.trustExitCode false
git config --global mergetool.unityyamlmerge.cmd \
  '"C:/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

`.gitattributes` routes Unity YAML files to `merge=unityyamlmerge` — that is a **merge
driver**, which the mergetool lines above do not define. Without the driver git silently falls
back to a line merge on exactly the files it must never line-merge, so set it too:

```
git config --global merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config --global merge.unityyamlmerge.driver \
  '"C:/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -h -p --force %O %B %A %A'
```

Adjust the path if your Unity install differs.

### Scene ownership

Tooling helps; discipline is what actually works.

- **One person edits a scene at a time.** Say so in chat before you open it.
- Prefer editing **prefabs** over scenes — prefab changes conflict far less.
- Never leave a scene half-edited on a branch overnight.
- If a scene conflicts anyway: **do not hand-merge the YAML.** Take one side whole
  (`git checkout --ours` / `--theirs`), reopen it in Unity, and redo the smaller change.
  Redoing ten minutes of work beats debugging a corrupted scene for two hours.

---

## 5 · Git LFS

Binary assets are tracked by LFS via `.gitattributes` — images, models, audio, video, fonts,
PDFs, native libraries.

Two things to know:

- **Adding a new binary type?** Add the extension to `.gitattributes` **before** committing
  the first file of that type. Files committed before the rule exists stay in normal git
  history forever.
- **GitHub's free LFS tier is 1 GB storage and 1 GB bandwidth per month.** Commit
  game-ready assets, not source files. Keep `.blend`, layered `.psd` and raw audio in
  external storage unless the project genuinely needs them versioned.

- **Exported art is the heavy part.** Sources stay in `LastSeenWearingArt` (D-004); only
  game-ready `.fbx`/`.png` enter the repo, and every re-export is a fresh LFS object. Batch
  re-exports and commit them when a pass is done, not after every render.

Check what LFS is tracking:

```
git lfs ls-files
```

---

## 6 · Documentation is committed, and it is not optional

`docs/` is shared and versioned. Keep it true in the same commit as the change:

- New system or folder → `docs/ARCHITECTURE.md`
- New config asset → `docs/DATA.md` catalogue
- Non-obvious decision → `docs/DECISIONS.md`
- Phase progress → `docs/STATUS.md`

A doc that describes a project that no longer exists is worse than no doc, because it is
trusted.

---

## 7 · `TODO.md` — personal, not shared

`TODO.md` is each developer's private task list. It is gitignored, so your agenda never
collides with the other developer's.

It is created automatically: `Assets/_Project/Scripts/Editor/TodoBootstrap.cs` runs on Unity
editor load, and if `TODO.md` is missing from the project root it copies `TODO.template.md`
into place and logs one line to the Console. If the file already exists it does nothing.

Team-wide progress belongs in [`STATUS.md`](STATUS.md), which **is** committed.

---

## 8 · Builds

**Last Seen Wearing → Build → Windows (Development | Release | Playtest)** in the Unity menu bar. Output goes to
`Builds/Windows_Dev/`, `Builds/Windows/` or `Builds/Windows_Playtest/` (gitignored), scenes come from the build list
(`Bootstrap` first), and Localization's Addressables content is built with the player. Playtest is a release build
compiled with `LSW_PLAYTEST`: no debug keys or readouts; what else it keeps on is decided before the first
playtest (P1.27). Code:
`Scripts/Editor/Tools/WindowsBuild.cs`.

---

## 9 · Recovery

| Problem | Fix |
|---|---|
| `Library/` got committed | `git rm -r --cached Library/`, verify `.gitignore`, commit |
| Broken references after a merge | Close Unity, delete `Library/`, reopen. Unity rebuilds it — it is derived data |
| A `.meta` was deleted | Restore it from git. Do not let Unity generate a new one; the GUID would change |
| Scene conflict | §4. Take one side, redo the change |
| Wrong Unity version opened the project | `git checkout ProjectSettings/`, reinstall the right version |

---

## 10 · Merging a long-lived branch

Carried over from Pane & Panic, where a six-day branch merged with 11 conflicting files — and the
two worst problems were not among them: both branches had used the same decision numbers, and both
had claimed the same serialized field. Git merged the second one silently, and the result compiled.

### While the branch is open

- **Say so in `STATUS.md` §1** on the day you branch: one line, "in flight on `branch`: what, GDD §".
  That line is what stops the other person building the same thing, or planning around it.
- **Merge `development` into your branch at the end of every session** (§2 already asks for "often").
- **The save schema is shared ground.** Before adding a field to `Core/Save`, check
  `development`'s copy of that file, not just yours.

### Before merging

Do all of this read-only. Nothing touches the working tree yet.

```
git fetch --all --prune
git merge-base development <branch>
git merge-tree --write-tree --name-only development <branch>      # the real conflict list
comm -12 <(git diff --name-only $BASE development | sort) \
         <(git diff --name-only $BASE <branch> | sort)             # touched on both sides
```

Then check the things a conflict list cannot show:

| Check | How |
|---|---|
| Decision numbers | `D-` headers added on each side since the merge base. If they overlap, **whoever merges second renumbers** |
| Save schema | Each side's added fields in `Core/Save` |
| Duplicate Unity callbacks and execution orders | `grep -c 'void LateUpdate\|void Update\|DefaultExecutionOrder'` on every auto-merged `.cs` |
| Input bindings | Both sides' `.inputactions` changes — the same key bound twice in one map |
| Components one side added to an existing prefab | Does a prefab the *other* side created need them too? Nothing errors when one is missing |
| GDD `[LOCKED]` items | Two branches that both changed a lock need a decision, with its rewrite cost (GDD §22) |
| Assembly directions | `references` in each `.asmdef`: Core → nothing, Gameplay → Core, UI → Core + Gameplay |
| Test count | Count on both sides. After the merge the suite should come to roughly the sum of both sides' additions |

### Merging

1. **Tag first:** `git tag pre-<branch>-merge development`. Close Unity.
2. **Renumber on a copy of the branch**, as its own commit, before the merge commit: one pass, never a
   chain of replaces (that way 58→62 can never also become 66), then grep to prove nothing else
   changed.
3. **Merge on `merge/<branch>`**, never straight onto `development`.
4. **Prefabs and scenes:** run UnityYAMLMerge (§4) into a scratch folder first. Accept its result only
   after checking that every object and every changed line from both sides is present. If it can't
   resolve a file, take one side whole and redo the other side's change in Unity.
5. **Shared docs:** `DECISIONS.md` keeps both sides, in numeric order. `STATUS.md` is **rewritten**,
   not line-merged: whoever merges writes §1 and compresses the branch's session log into one table
   (what landed · GDD § · roadmap phase · decision · played?). The detail already lives in
   `DECISIONS.md`.
6. **Read the auto-merged files that both sides touched.** "No conflict" only means the changes were
   on different lines, not that they work together.
7. **Record the merge's own calls** in one `DECISIONS.md` entry.
8. Compile, run the suite, play the build — only then merge `merge/<branch>` into `development`.
