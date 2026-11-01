# Portfolio Game Development Guide

The process and technical rules for building every portfolio game, from first idea to a playable, documented release.

Adapted from the Foxtale Studio process (Project Setup, Unity Coding Standards, GDD rules, Technical Documentation, Game Feel Playbook, `prototype-fast`, and Killer Vibe's `Docs/` + `CLAUDE.md`). Changes for portfolio work:

- Game docs live in the user's **personal Notion**, one page per game under **Games** (§2). Only the Claude process files (`CLAUDE.md`, `Docs/PROCESS.md`) stay in the repo. *(Changed 2026-11-01; previously Markdown in each repo.)*
- **Git + GitHub** instead of Plastic.
- **Platform is chosen per game** (PC/WebGL or Android).
- There is no Voodoo brief, TinySauce, CPI or store-metric step. A **case study** replaces them, because the audience is a recruiter, not a publisher.

> **How to use this file.** Copy it once into each new repo as `Docs/PROCESS.md`, or keep one master copy and link to it from each game's `CLAUDE.md`. Section 13 holds every template. When a rule here changes, change the master copy and add a line to its Change Log at the bottom.

---

## 0. The ten rules that matter most

1. **One question per prototype.** Every game starts as a prototype that answers one question: *is this verb fun?* Write keep/kill criteria before writing code (§4).
2. **Docs are reference, not a build order.** Nothing gets built just because a task list says it comes next. You decide; Claude implements.
3. **The GDD says *what*, the Tech Doc says *how*.** A new feature goes into the GDD first. No design decisions are made in the Tech Doc.
4. **The project wins over the page.** If code and the Tech Doc disagree, fix the doc and say so in one line.
5. **Update the Tech Doc as you work**, not at the end. Write it for a stranger: a new session with no chat history must be able to pick up any open task from it alone.
6. **Simple, flat, boring code** (§7). Plain MonoBehaviours, `[SerializeField] private`, direct calls. No pattern without a second real use.
7. **No hardcoded tunables.** Any number a designer would change lives in a ScriptableObject.
8. **Feedback on every key moment.** Each core action gets at least four game-feel signals (§9).
9. **Stable IDs, never renumbered.** `T-` tasks, `B-` bugs, `U-` unconnected, `D-` decisions, `P-` playtests. Mark items done or fixed; never delete them.
10. **Every game ends in a case study** (§12). A finished game nobody can understand in 30 seconds is not a portfolio piece.

---

## 1. Lifecycle of a portfolio game

| Phase | Output | Gate to the next phase |
|---|---|---|
| **0. Pitch** | `Docs/00_Pitch.md` (§13.1) | All four sanity checks answered |
| **1. Prototype** | Greybox spike + filled prototype brief | Keep / Kill / Refactor decision logged |
| **2. GDD** | `Docs/01_GDD.md` (§13.2) | Scope fits the timebox; red flags resolved |
| **3. Setup** | Repo, folders, packages, settings (§5–§6) | Empty project builds and runs on the target platform |
| **4. Vertical slice** | Core loop playable start to end, with greybox art | One full session is playable by a stranger |
| **5. Feel & polish** | Feedback map built (§9), real art, audio, UI | Playtest P-xx passes the GDD's success criteria |
| **6. Ship** | Build on itch.io / APK, tagged release (§11) | Ship checklist all ✅ |
| **7. Case study** | `README.md` front page + media (§12) | A reviewer gets it in 30 s |

**Timeboxes (suggested):** prototype 1–3 days · slice 1–2 weeks · polish 1 week · ship + case study 2–3 days. Every portfolio game should be finishable in **4–6 weeks**. If the GDD needs more, cut scope, not quality.

---

## 2. Repository layout

```txt
<GameName>/                    ← Unity project root = git repo root
├── README.md                  ← portfolio front page / case study (§12)
├── CLAUDE.md                  ← short pointer file for Claude (§13.6)
├── .gitignore  .gitattributes ← Unity ignore + Git LFS rules (§6)
├── Docs/
│   ├── PROCESS.md             ← this file (the only doc that stays in the repo)
│   ├── Prototypes/            ← one brief per prototype question
│   └── Media/                 ← GIFs, screenshots, diagrams used by README
├── Assets/ … (§5)
├── Packages/  ProjectSettings/
└── Builds/                    ← ignored by git
```

**Game docs in Notion** (personal workspace, never the Foxtale one):

```txt
Games/
└── <Game Name>/               ← one page per game; CLAUDE.md links each page below
    ├── Pitch                  ← the idea, sanity checks
    ├── GDD                    ← living design doc (what)
    ├── Tech Doc               ← living technical doc (how) — session hand-over
    ├── Functionality          ← how each built feature works, in plain words (§10.3)
    ├── Checklist (database)   ← Status Open / In progress / Done, Order, Sub-items (§10.4)
    ├── Playtests              ← playtest log, P-01, P-02 …
    └── Ideas (date)           ← brainstorm reports
```

Wherever this file names `Docs/00_Pitch.md`, `Docs/01_GDD.md`, `Docs/02_TechDoc.md`, `Docs/03_Playtests.md`, `Docs/04_Functionality.md` or `Docs/05_Checklist.md`, read it as the matching Notion page.

The GDD and the Tech Doc must never repeat each other. Link to the other doc instead (`see GDD §5`).

---

## 3. Working with Claude

### 3.1 Start of every session (Claude)

1. Read `CLAUDE.md`, then `Docs/02_TechDoc.md` **in full** before any Unity task. Start from *Status snapshot*, *Open tasks*, *Unconnected / stubbed* and *Bugs*.
2. Read the GDD sections that own the feature being worked on, and say which ones.
3. Say in one line anything still pending from the last session (unconnected items, uncommitted work).

### 3.2 Standing rules

- **You decide, Claude implements.** Nothing is created unless you ask: no files, scripts, scenes, prefabs or folders "while I was in there".
- **Scenes: always ask first.** Before creating or editing a scene, or changing the build order, ask which scene is which. Never infer a scene's role from its name.
- **Feature not in the GDD, or contradicting it?** Stop and ask whether to add or change it. Then update the GDD, bump its version and add a Change Log line.
- **Feedback-first.** With every gameplay change, propose the player feedback for it (§9) as one short list with a recommended pick. Build only what you approve.
- **After code:** say which GameObject or prefab gets the script, what to wire in the Inspector, and how to test it in Play mode.
- **Git:** never commit or push unless asked. Work on a feature branch, not `main`.
- **Chat:** short and plain, answer first. One question at a time, with a recommended answer.
- **Use `game-trend-skills`** (user rule, 2026-11-01) wherever one fits, and say in one line which one: `brainstorming-game-ideas` for pitches (§13.1); `evaluating-game-ideas` for the keep/kill call (§4) and continue / pivot / drop; `scanning-game-trends` and `scouting-social-trends` for GDD "Similar games"; `launching-on-zero-budget` for shipping and the itch page (§11–§12); `tracking-game-metrics` for playtests and launch numbers; `writing-game-agent-briefs` for session or agent briefs. Not for pure coding or scene work.
- **Inferences in docs** get tagged on the next line: `[AI-CHOICE: chose X over Y — reason]`. You review these and either remove the tag or change the text.

### 3.3 Conflict flag

If something you say contradicts the GDD, Claude stops and writes:

```
⚠ CONFLICT — GDD §x
Doc says: <current rule>
You said: <new instruction>
Affects:  <other sections / systems / code that would change>
```

Nothing gets written until you choose a version. If you take the new one, update the GDD first, then the code, in the same commit.

---

## 4. Prototyping (keep / kill)

The output of a prototype is a **decision**, not a product.

1. **Write the one question.** If there are two questions, make two prototypes.
2. **Decide throwaway or keep before coding.** Most prototypes are throwaway: hard-code freely and keep them in `Assets/_Prototypes/<Idea>/` or a separate project.
3. **Hard timebox:** 30–90 minutes for a mechanic, one sitting for a slice.
4. **Greybox everything** except the core verb: primitives, the built-in font, no art.
5. **Instrument for the question.** On-screen debug text and gizmos for the numbers you're judging.
6. **Playtest immediately.** Play it yourself, then hand it to one person without explaining anything.
7. **Make the call against the criteria:**
   - **Keep** when a fresh player uses the verb on purpose within ~30 s, unprompted, and asks for "one more go".
   - **Kill** when it only feels good after you explain it, needs systems far beyond scope, or you catch yourself adding art to hide a flat verb.
   - **Refactor** when one variable is clearly off: retune and retest.
8. **A keep authorises a rewrite** in `_Main/`, not a copy-paste of the spike.

Fill in the brief (§13.3) before starting. Log the outcome in the brief and in Tech Doc §10 *Decisions*.

---

## 5. Unity project setup

### 5.1 Setup order (one step at a time; "already done" is always an option)

1. Pitch + prototype decision is **Keep**.
2. Create the Unity project and the GitHub repo. Add `.gitignore` and `.gitattributes` **before the first commit** (§6).
3. Folder structure (§5.2).
4. Packages (§5.4).
5. Project settings (§5.5).
6. Scenes: agree which scene is which and set the build order (§5.6).
7. Create `Docs/` from the templates (§13) and seed the Tech Doc from the GDD.

Log each finished step in the Tech Doc (close its `T-` task).

### 5.2 Folder tree

```txt
Assets/
├── _Main/                  ← everything we make
│   ├── Player/   (Scripts/, Prefabs/, Materials/ … as needed)
│   ├── Enemies/
│   ├── Characters/         ← Clock Out: player + coworker prefabs, models, animator
│   ├── World/
│   ├── UI/
│   ├── Audio/
│   ├── Data/               ← ScriptableObjects: tuning, balance, config
│   ├── Scenes/
│   └── Shared/             ← materials, shaders, fonts used everywhere
├── _Prototypes/            ← throwaway spikes; deleted or never referenced by _Main
├── Imported Assets/        ← every Asset Store / marketplace pack
│   ├── Character/  Environment/  UI/  VFX/  Audio/
└── Tests/                  ← EditMode/ and PlayMode/ test assemblies
```

- Scripts live **inside the feature folder** (`_Main/Player/Scripts`). There is no global Scripts folder.
- Ask before adding a new top-level `_Main` folder, then add it to this tree.
- **Never edit files inside `Imported Assets`.** Duplicate what you need into `_Main` and change the copy.
- SDK and package folders stay where they install (`Plugins`, `TextMesh Pro`, URP `Settings`).
- Feature folders are per game. The list above is the default; the GDD can rename them (e.g. `Enemies/` → `Guards/`).

### 5.3 Naming: `<Prefix>_<Name>_<Type>`

`<Prefix>` = the game's initials, chosen at project start and recorded in the GDD header. Scripts and scenes **must** follow the pattern. Other assets should follow it where practical. The class name equals the file name. Imported assets keep their original names.

| Asset | Suffix | Example |
|---|---|---|
| C# script | `_Script` | `CO_GuardPatrol_Script` |
| Scene | `_Scene` | `CO_Warehouse_Scene` |
| Prefab | `_Prefab` | `CO_Guard_Prefab` |
| Material | `_Mat` | `CO_Floor_Mat` |
| Texture / Sprite | `_Tex` / `_Sprite` | `CO_Alert_Sprite` |
| Anim clip / Animator | `_Anim` / `_Animator` | `CO_GuardWalk_Anim` |
| ScriptableObject asset | `_Data` | `CO_GuardTuning_Data` |
| Audio | `_SFX` / `_Music` | `CO_Footstep_SFX` |
| Shader / Shader Graph | `_Shader` | `CO_Outline_Shader` |
| Input Actions | `_Input` | `CO_Player_Input` |

**Existing projects:** apply the convention to new and changed code only. Don't rename working code just to match it, unless you schedule a rename task (`T-xx`) on purpose.

### 5.4 Packages

| Package | When |
|---|---|
| Input System | Always |
| TextMeshPro + uGUI | Always (UI Toolkit only if the GDD chooses it) |
| URP | Always (default render pipeline) |
| AI Navigation | Any game with NavMesh agents |
| Cinemachine | When the camera needs more than a simple follow |
| DOTween (Pro) | UI animation and small code tweens (pop, fade, slide, punch). Default for any UI motion |
| Feel (+ Nice Vibrations) | Layered game-feel moments (freeze, camera shake or nudge, flash, sound + particles together) built as `MMF_Player`s; haptics on Android. Use it where several signals fire at once; a single tween stays DOTween |
| A UI art pack (e.g. HKProductions Premium Casual GUI) | Sprites, frames, badges, buttons and fonts for every screen and world-space marker, instead of plain Unity shapes |
| Unity Test Framework | Always: at least one PlayMode test for the core loop |

Anything else needs your OK first and a `D-` entry in the Tech Doc.

**Using them:** packs stay in `Assets/Imported Assets/` (or `Plugins/`) and are never edited; build our prefabs in `_Main` that use their sprites, fonts and components. Precompiled `DOTween.dll` is visible to every asmdef; DOTween's UI module shortcuts (`Image.DOFade`, `CanvasGroup.DOFade`) are not, so tween those with `DOTween.To`. Always `SetLink(gameObject)` so tweens die with their object. Feel compiles into the `MoreMountains.Tools` assembly: an asmdef that drives an `MMF_Player` must reference it (ask first, like any assembly change). Feedback scripts read gameplay state and never write it (§7.2).

### 5.5 Project settings

**Every game**

| Setting | Value |
|---|---|
| Unity | 6000.3.x LTS (record the exact version in the GDD header) |
| Asset Serialization | Force Text |
| Version Control mode | Visible Meta Files |
| Color Space | Linear |
| Active Input Handling | Input System Package (New) |
| Enter Play Mode Options | Leave at the default unless a script is audited for domain-reload safety |
| Target frame rate | Set once at boot in code; 60 by default |
| Company Name / Product Name | Your name or studio name / the game name from the GDD |
| Version | Semantic, `0.1.0`; bump on each release |
| Unity splash | Off (free on Unity 6) |

**PC / WebGL games (itch.io)**

| Setting | Value |
|---|---|
| WebGL compression | Brotli, with **Decompression Fallback on** (itch serves without the right headers) |
| WebGL template | Minimal or a custom one at the GDD's resolution; 16:9 default (1280×720 or 1920×1080) |
| Code optimisation | Disk size with LTO for release builds |
| Controls | Keyboard + mouse; gamepad optional. All controls are shown on screen or in a pause menu |
| Windows build | IL2CPP, x86_64, windowed + fullscreen toggle |

**Android games**

| Setting | Value |
|---|---|
| Scripting Backend | IL2CPP, ARM64 only |
| Target API | Latest Google Play requirement |
| Api Compatibility | .NET Standard 2.1 |
| Orientation | From the GDD. Portrait only, or landscape left + right |
| Package Name | `com.<yourname>.<gamename>` (lower case, no spaces), fixed after the first upload |
| Build output | APK for test devices / itch; AAB only for Google Play |
| Quality | URP mobile quality level as the Android default |
| Optimized Frame Pacing | On |
| Keystore | Your own release keystore. Never commit it or write its passwords in any doc |

### 5.6 Scenes and build order

| Index | Scene | Notes |
|---|---|---|
| 0 | `<P>_Boot_Scene` *(optional)* | Sets the frame rate and loads settings, then the next scene |
| 1 | `<P>_MainMenu_Scene` | Title, Play, Settings, Credits, Quit (PC) |
| 2 | `<P>_Game_Scene` | Or one scene per level |
| 3+ | Other levels / tutorial | Ask before adding |

Load "the next scene" by build index where possible, so unticking a placeholder scene removes it from the flow without code changes. Record the scene roles in Tech Doc §3.

---

## 6. Git + GitHub

- **Before the first commit:** add Unity's `.gitignore` (ignores `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.sln*`) and a `.gitattributes` that sends binaries to **Git LFS**: `*.png *.jpg *.psd *.tga *.fbx *.blend *.wav *.mp3 *.ogg *.mp4 *.mov *.ttf *.otf *.unitypackage`, plus `*.unity *.prefab *.asset merge=unityyamlmerge` if you set up Smart Merge.
- **Imported paid packs:** check the licence. Most Asset Store packs **can't be redistributed in a public repo**. Either keep the repo private and publish only builds plus selected source, or gitignore `Assets/Imported Assets/` and list the packs in the README so others can import them.
- **Branches:** `main` is always buildable. Work on `feature/<short-name>`, merge when the feature runs in Play mode.
- **Commits:** imperative and specific (`Add guard hearing radius to tuning data`). Docs and code that belong together go in the **same commit**. A commit where the code and the Tech Doc disagree is how the docs rot.
- **Releases:** tag each shipped build `v0.1.0` and attach the build or link to the itch page in the GitHub Release notes.

---

## 7. Coding standards

You are writing code a reviewer will read. Optimise for **clarity and speed of delivery**, not abstract architecture.

### 7.1 Philosophy

1. **Direct and simple first.** The most straightforward code that solves the problem. No premature optimisation.
2. **Flat.** One script per feature or screen. Split only past ~500 lines or when it becomes hard to read.
3. **Minimal dependencies.** Unity API + .NET + the packages in §5.4. Don't wrap Unity calls (`Instantiate`, `Debug.Log`, `PlayerPrefs`) in helpers.
4. **YAGNI.** Build exactly the GDD feature in front of you. No options, hooks or generic systems "for later".
5. **No fluff.** Comments explain *why*, in one line, never *what*.
6. **Reviewable.** Short methods, obvious names, no clever one-liners.

### 7.2 What that means in Unity

- Plain `MonoBehaviour`s with `[SerializeField] private` fields wired in the Inspector, and direct references with plain method calls.
- **Allowed when earned:** an interface when **two or more** real classes implement it (e.g. `IInteractable` on doors, switches and pickups); a C# `event`/`UnityEvent` when **one source has several unrelated listeners**. Not allowed: DI frameworks, service locators, global event buses, single-implementation interfaces, deep inheritance.
- **ScriptableObjects for tuning and data only**, never as an architecture. Every designer-facing number lives in a `_Data` asset under `_Main/Data/`. Exceptions are logged in Tech Doc §10.
- **Premade over runtime-built.** Objects live in scenes and prefabs. Use `Instantiate` for things that spawn in gameplay (projectiles, VFX, enemies), and pool them only when the profiler says so.
- **No actor-type branching in shared systems.** Never write `if (isPlayer)` inside a system that the player and the AI both use. Put the difference in data or in the controller that drives it ("one body, two drivers").
- **Presentation reads, never writes.** Animation, VFX and UI scripts observe gameplay state and never change it. This keeps feel work from breaking rules.
- **Unity specifics:** cache component lookups in `Awake`; no `Find`/`GetComponent` in `Update`; no `SendMessage`. Use `CompareTag`. Physics in `FixedUpdate`, input in `Update`. Unsubscribe events in `OnDisable`.
- **Debug on purpose.** Draw gizmos (`OnDrawGizmosSelected`) for every radius, cone and path an AI uses. A visible vision cone in a GIF is portfolio gold.
- **Tests:** at least one PlayMode test for the core loop and EditMode tests for any pure logic (scoring, state transitions). They are both a safety net and a portfolio signal.
- **Breaking a rule** (new package, a pattern, a split) needs your OK and a `D-xx` entry in Tech Doc §10.

### 7.3 Style

- `PascalCase` for classes, methods and properties; `camelCase` for fields and locals; `_camelCase` is not used. Booleans read as questions (`isAlerted`, `hasKey`).
- Group the Inspector with `[Header("…")]` and add `[Tooltip("…")]` to any field whose unit isn't obvious (`metres`, `seconds`, `degrees`).
- `#region` is not used. If a file needs regions, it needs splitting.

---

## 8. The GDD (`Docs/01_GDD.md`)

The **living** design document. It answers *what the game is and how it should play*.

### 8.1 Writing flow (Claude, with you)

1. Summarise the pitch back in five lines or fewer.
2. Ask questions in topic rounds (up to 4 per round, each with a recommended answer). Skip anything already answered. Rounds: **Core** (verb, camera, session length, win/lose) → **Systems** (AI, abilities, progression) → **Screens** → **Art & tone** → **Scope**.
3. **Flag, never silently decide:**
   - 🚩 **Red flag:** too big for the timebox, unreadable on the platform, heavy physics or AI cost, needs online multiplayer.
   - ⚔️ **Contradiction:** features that fight each other.
   - 📏 **Platform break:** controls or UI that don't work on the chosen platform.

   Each goes in GDD §14 with your decision.
4. **Compare with 2–4 similar games:** what makes each fun and what to take. End with "Focus on these to make it fun" (3–5 items).
5. **Feedback map** (§9): the moments that need the most feedback, ranked.
6. Write section by section from the template. Every inference gets an `[AI-CHOICE: …]` tag.

### 8.2 Living GDD

- Any feature that comes up and isn't in the GDD, or contradicts it: **stop, ask, update the GDD, bump the version, add a Change Log line**. Only then code.
- Change Log line format: `- **YYYY-MM-DD — <what changed>** — <why>`.
- Cut features move to §15 *Cut / later* with the reason. They are never silently deleted. Showing what you cut and why is strong portfolio material.

---

## 9. Game feel

A mechanic can work and still feel like nothing. Players read impact through short, layered signals **on the exact frame** something happens. Most of the gain comes from timing and layering, not from more particles.

### 9.1 Cartoon rules

1. **Anticipation:** a short wind-up before the action.
2. **Squash and stretch:** fast things stretch; what they hit squashes.
3. **Follow-through:** bodies settle, trails lag, bursts keep flying.
4. **Timing:** hold the key frame with a hit freeze.
5. **Arcs:** nothing flies straight.

### 9.2 Recipes

| Moment | Signals, in order |
|---|---|
| **Melee hit** | white flash → sharp sound on the impact frame → directional particles → knockback (+ small attacker recoil) → tiny shake + hit freeze. Finisher: pop / burst |
| **Gunfire** | gun kick + muzzle flash + instant result → tracer/spread → hit flash + squash + knockback → shooter recoil + camera kick → leftovers (shells, marks) → layered sound (crack, whiz, thud, ping) |
| **Projectile / spell** | charge glow → core → trail → burst on impact → visible target reaction → whoosh/boom → aftermath |
| **Reward / pickup** | wobble faster → squash → stretch → slam open → inner light + rays → floor shockwave → star pop → floating dust. Scale with value |
| **AI alert (stealth)** | indicator pops (anticipation) → sting sound → brief camera nudge toward the guard → guard head-snap + pause → HUD state colour → (on lose) "?" fade + relief sound |
| **Detected / fail** | freeze 4–6 frames → desaturate or vignette → heavy sound → slow zoom on the cause → clear retry prompt |

### 9.3 Starting values (60 fps, 1 frame ≈ 0.017 s)

| Signal | Light | Heavy / finisher |
|---|---|---|
| White flash | 2–3 frames | 3–5 frames |
| Hit freeze | 2–3 frames (~0.04 s) | 4–6 frames (~0.08–0.1 s) |
| Screen shake | Tiny, very short | Small, short |
| Pitch variation | ± a few % | same |

### 9.4 Checklist

For every moment in GDD §11 *Feedback map*, tick: anticipation · flash · freeze · directional particles · knockback · shake · layered sound with pitch variation · haptic (Android) / controller rumble (PC) · aftermath. **A key moment needs four or more.** Keep it on-tone and readable.

---

## 10. The Tech Doc (`Docs/02_TechDoc.md`)

The **hand-over note between sessions**. It records how the project is actually built, how the parts connect, what is unconnected or broken, and how to do what's left.

### 10.1 When it's created

At setup, seeded from the GDD: planned systems (status *Not started*), scene order, a short "how it should work" per core system (linking the GDD section), and the setup steps plus the slice systems as open tasks. Script names and values that don't exist yet stay `TBC`. Never invent them.

### 10.2 When it's updated: as you work

| Something happened | Update |
|---|---|
| System, script or prefab added or changed | §2 *Architecture* row; §6 *Wiring* if references or events changed |
| Built but not hooked up (stub, placeholder, missing reference, uncommitted) | §7 *Unconnected* — what, where, exactly how to connect it |
| Bug found / fixed | §9 *Bugs* — repro steps, likely cause, status → *Fixed YYYY-MM-DD — fix* (keep the row) |
| Work left or next step clear | §8 *Open tasks* — dependencies and **how to do it from what already exists** (script names, where to hook in) |
| Technical choice made | §10 *Decisions* — choice, alternative, reason |
| You explain how something should work | §5 *How it should work* (and the GDD if it's a design change) |
| Every session | §1 *Status snapshot* + one line in §11 *Session log* |

Claude updates without asking and says so in one line ("Logged B-04 in the Tech Doc"). Trivial tweaks (a colour, a spacing) are skipped. At session end, Claude does a final check that nothing was missed.

**Style:** tables over prose, short entries, code identifiers exactly as written, real script, method and Inspector field names. Link to the GDD instead of repeating it.

### 10.3 The Functionality doc (`Docs/04_Functionality.md`)

A plain description of how each **built** feature works right now, step by step, as a player or tester sees it. One entry per feature (`FN-xx`, stable IDs): *what it does*, *how it works*, *rules / edge cases*, *refs*. The GDD says what it should be, the Tech Doc how the code does it; this doc says what it does today. Link to them instead of repeating them.

- **Before** changing an existing feature or adding a new one, Claude reads its entry (and the entries it touches) and says which ones.
- **After** a feature is added, changed or removed, Claude updates the entry in the same session, updates *Last updated*, and adds a Change Log line. A removed feature stays as an entry with status *Removed*.
- Planned features don't get an entry until they're built.

### 10.4 The Checklist (`Docs/05_Checklist.md`)

A Notion database. Each row has a Status (Open / In progress / Done), an `Order` (build order: main checks 10, 20, …; their sub-items 11, 12, …), a Scope (v1 / Stretch) and `Refs` pointing to its `T-`, `FN-` or GDD section rather than describing it. Main checks hold their sub-checks through `Sub-items` / `Parent item`.

- **"What's next?"** → query the database: the first row by `Order` with Status "In progress", or else the first "Open" one.
- When work on an item starts, Claude sets it to In progress. When it's finished, Claude sets it to Done, and a main check is Done only when all its sub-items are.
- A new feature added to the GDD gets a checklist item in the same session.

---

## 11. Playtesting and shipping

### 11.1 Playtests (`Docs/03_Playtests.md`)

- Each session gets an ID `P-xx`: build version, tester profile, what you asked them to do (nothing more), and what you watched for.
- Record **observations, not opinions**: "missed the door prompt 3×", not "UI bad".
- Each finding becomes a `B-`, a `T-` or a GDD change, with the ID linked back.
- Minimum before shipping: **3 testers who have never seen the game**.

### 11.2 Ship checklist

| Check | ✅ |
|---|---|
| Title, Play, Settings (volume, fullscreen, sensitivity), Credits, Quit (PC) | |
| Controls shown in-game; pause menu works | |
| A full session has a clear start, goal and end (win/lose screen, restart) | |
| No errors in the Console during a full playthrough of the build | |
| Holds the target frame rate on the target device / browser | |
| WebGL: loads on itch in Chrome + Firefox, fullscreen button works, audio starts after first click | |
| Android: tested on a real device, safe areas respected, back button handled | |
| Version bumped, git tag created, Tech Doc §1 updated | |
| Credits list every imported pack, font and sound with its licence | |
| itch page: cover image (630×500), 3–5 screenshots, a GIF, short description, controls | |

---

## 12. Portfolio presentation (`README.md`)

The README is the case study. A reviewer spends **30 seconds** on it. Order:

1. **Title + one-line pitch + hero GIF** (5–10 s of the core verb with the best feedback moment).
2. **Play it:** itch link / build download. Platform, engine version, time spent, team (solo / role).
3. **What I built:** 3–5 bullets on the systems you own, with the interesting part named ("guard AI: patrol → hear → investigate → chase state machine on NavMesh, with a debug vision cone").
4. **Technical highlights:** one short paragraph and a diagram or GIF for each of the 1–2 hardest problems: the problem, the options, what you chose and why (pull from Tech Doc §10).
5. **Design process:** pitch → prototype question → keep/kill result → what playtests changed (pull from `03_Playtests.md` and GDD §15 *Cut*).
6. **What I'd do next:** 2–3 honest lines.
7. **Credits and licences.**

Links to `Docs/` at the bottom show the process. The README itself stays short.

---

## 13. Templates

### 13.1 `Docs/00_Pitch.md`

```markdown
# <Game Name> — Pitch

**One line:** <verb + goal + twist, under 25 words>

## Toy + Rules
| | |
|---|---|
| Core verb / input | <the single action the player repeats> |
| Why it feels good | <the sensation> |
| Rules reference | <genre / reference game the ruleset borrows from> |
| Win / lose | |
| Session length | |

## Sanity check — answer all four or the idea isn't ready
| Check | Answer |
|---|---|
| 🎯 Distinct — what's the one thing no similar game does? | |
| 🧠 Depth — where do player decisions matter? | |
| 👀 Readable — is the fun obvious in 3 seconds of a GIF? | |
| 🧰 Showcase — which skill does this prove to a reviewer (AI, physics, tools, UI, feel)? | |
| ⏱ Scope — finishable in 4–6 weeks solo? | |

## Prototype question
<The one yes/no question the first prototype must answer.>
```

### 13.2 `Docs/01_GDD.md`

```markdown
# <Game Name> — GDD
**Version:** 0.1 · **Prefix:** `XX_` · **Unity:** 6000.3.x · **Platform:** PC/WebGL | Android (orientation) · **Status:** Prototype | Slice | Polish | Shipped

## 1. Overview — pitch, core fantasy, references
## 2. Pillars — 2–3 design pillars every feature must serve
## 3. Core loop — moment-to-moment, then session loop (diagram welcome)
## 4. Controls — per platform, one table
## 5. Mechanics — one subsection per mechanic: rule, numbers (link to `_Data` asset), edge cases
## 6. AI / enemies — states, what they perceive, how they feel fair
## 7. Levels / world — layout rules, how many, how built
## 8. Progression / scoring — if any
## 9. Screens & UI — screen → purpose → v1? → notes
## 10. Art & audio direction — tone, style, references
## 11. Feedback map — moment → signals (§9 checklist) → priority
## 12. Similar games — what each does well · what we take · "Focus on these to make it fun"
## 13. Success criteria — what playtests must show for the game to be "done"
## 14. Flags — 🚩 / ⚔️ / 📏 · issue · decision · date
## 15. Cut / later — feature · reason · date
## Change Log
```

### 13.3 `Docs/Prototypes/<NN>_<Idea>.md`

```text
QUESTION:     The one thing this must prove (yes/no).
CORE VERB:    The single action the player repeats.
THROWAWAY?:   yes → hard-code freely, delete after | no → minimal structure to grow.
TIMEBOX:      e.g. 90 min. Stop when it rings.
INSTRUMENTS:  Debug text / gizmos shown on screen.
KEEP IF:      Observable signal.
KILL IF:      Observable signal.
RESULT:       Keep | Kill | Refactor — date — what testers did — lesson.
```

### 13.4 `Docs/02_TechDoc.md`

```markdown
# <Game Name> — Technical Documentation

> **Read me first (Claude).** Read this whole page before any Unity task. The project wins over this page:
> if code differs, fix the page and say so. Update as you work. Design lives in `01_GDD.md`; link, don't repeat.

## 1. Status snapshot
| Field | Value |
|---|---|
| Last updated | YYYY-MM-DD — <what> |
| Phase | Prototype / Slice / Polish / Shipped |
| What works now | |
| Uncommitted / unpushed | |
| Next up | |

## 2. Architecture
| System | Scripts / prefabs | Responsibility | Connects to | Status |
|---|---|---|---|---|
(Status: Not started · In progress · Works · Done)

## 3. Scenes and build order
| Index | Scene | Role | Notes |

## 4. Data, save and config
| Asset (`_Data`) | Holds | Used by |

## 5. How it should work
### <System> (GDD §x) — short spec: states, timings, edge cases

## 6. Wiring
| From | To | How (Inspector ref / event / call) |

## 7. Unconnected / stubbed
| ID | What | Where | How to connect |
| U-01 | | | |

## 8. Open tasks
| ID | Task | Depends on | How to do it (from what exists) | Status |
| T-01 | | | | |

## 9. Bugs / mismatches
| ID | Bug | Repro | Likely cause | Status |
| B-01 | | | | Open / Fixed YYYY-MM-DD — fix |

## 10. Technical decisions
| ID | Decision | Alternative | Reason | Date |
| D-01 | | | | |

## 11. Session log
- YYYY-MM-DD — <one line: what changed>
```

### 13.5 `Docs/03_Playtests.md`

```markdown
| ID | Date | Build | Tester | Task given | Observations | Actions (B-/T-/GDD) |
|---|---|---|---|---|---|---|
| P-01 | | v0.1.0 | first-time, plays FPS | "Reach the exit" | | |
```

### 13.6 `CLAUDE.md` (pointer file, keep it short)

```markdown
# <Game Name> — Claude project notes

Portfolio game by <you>. **You decide, Claude implements.** The docs in `Docs/` are the source of truth;
this file only points there. If this file disagrees with them, the docs win: say so in one line.

## Start of every session
1. Read `Docs/02_TechDoc.md` in full before any Unity task.
2. Read the `Docs/01_GDD.md` sections that own the task, and name them.
3. Follow `Docs/PROCESS.md` (this guide): coding standards §7, game feel §9, Tech Doc rules §10.

## Project facts
- Unity 6000.3.x, URP, <platform + orientation>, Input System, uGUI + TMP.
- Prefix `XX_`. Our files in `Assets/_Main/<Feature>/`; packs in `Assets/Imported Assets/` (never edit).
- Git + GitHub. Feature branches; never commit or push unless asked.
- Scenes: ask before creating or editing any scene. Roles are in Tech Doc §3.

## Game-specific rules
- <anything only this game needs>
```

---

## Change Log

- **2026-11-01 — §5.4 packages:** DOTween, Feel and a UI art pack are the defaults for UI motion, layered game feel and UI art, with usage rules.
- **2026-11-01 — Created** from the Foxtale Studio process (Notion: Project Setup, Unity Coding Standards, GDD, Technical Documentation, Game Feel Playbook; Killer Vibe `CLAUDE.md` and `Docs/`; Snow Rivals `CLAUDE.md`; `prototype-fast` skill). Adapted for solo portfolio work: Markdown in the repo, Git + GitHub, platform per game, lean process, case-study README.
- **2026-11-01 — §3.2: use `game-trend-skills` wherever one fits.** — User rule for all future tasks.
- **2026-11-01 — §2, §10.3: added `Docs/04_Functionality.md`** — User rule: read it before changing or adding a feature, and update it whenever one is added or changes.
- **2026-11-01 — §2, §10.4: added `Docs/05_Checklist.md`** — User rule: a ✅ / 🟡 / ⬜ build list that answers "what's next?".
- **2026-11-01 — §0 intro, §2, §10.4: game docs moved to personal Notion** — User rule: Games → one page per game; only `CLAUDE.md` and `Docs/PROCESS.md` stay local. The Checklist became a Notion database.
