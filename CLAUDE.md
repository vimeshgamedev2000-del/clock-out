# Clock Out — Claude project notes

Portfolio stealth game **Clock Out**. **You decide, Claude implements.** The game docs live in the user's
**personal Notion** (workspace "Vimesh Shahama's Space", page Games → Clock Out) and are the source of truth.
This file only points there. If this file disagrees with them, the docs win: say so in one line.

## Game docs (Notion, read and write them with the Notion connector)
| Doc | Notion page |
|---|---|
| Game home | https://app.notion.com/p/40b9abae45dc8158a62df46423be3d6d |
| Tech Doc | https://app.notion.com/p/40b9abae45dc81838588d54336293a9b |
| GDD | https://app.notion.com/p/40b9abae45dc81adbd33fcc9be987cab |
| Functionality | https://app.notion.com/p/40b9abae45dc817c8a75d69b762b8a5e |
| Checklist (database) | https://app.notion.com/p/7015961433b641dc96f4b2bde02f5574 · data source `collection://33192361-925e-412a-84ee-693ecf7fd69b` |
| Pitch | https://app.notion.com/p/40b9abae45dc814bb376d3ee0a0e0bd5 |
| Playtests | https://app.notion.com/p/40b9abae45dc813b92c3dab20ee19f91 |
| Ideas (2026-11-01) | https://app.notion.com/p/40b9abae45dc81a08c22f67e99a73304 |

Before the first Notion call, run `notion-get-self`. If the workspace isn't "Vimesh Shahama's Space" (e.g. it's Foxtale Studio's), stop and ask the user to connect their personal Notion. Never write game docs to the Foxtale workspace. If Notion can't be reached, say so; don't recreate the docs locally.

## Start of every session
1. Read the **Tech Doc** page in full before any Unity task.
2. Read the **GDD** sections that own the task, and name them.
3. Before changing an existing feature or adding a new one, read its entry on the **Functionality** page, and update that page in the same session whenever a feature is added, changed or removed (`PROCESS.md` §10.3).
4. Follow `Docs/PROCESS.md`: coding standards §7, game feel §9, Tech Doc rules §10.
5. "What's next?" → query the **Checklist** database: the first item by `Order` with Status "In progress", or else the first "Open" one. Set Status (Open / In progress / Done) as work happens; a main item is Done only when all its Sub-items are (`PROCESS.md` §10.4).
6. For tasks about ideas, design evaluation, trends, launch or metrics, use the matching `game-trend-skills` skill (`Docs/PROCESS.md` §3.2).

## Project facts
- Unity 6000.3.19f1, URP, PC + WebGL (itch.io), landscape 16:9, Input System, uGUI + TMP, AI Navigation.
- Prefix `CO_` for scripts, scenes and our assets (`CO_<Name>_<Type>`, e.g. `CO_EnemyAI_Script`). All our scripts and `_Main` assets use it since 2026-11-01. The Unity folder is still `AI Patrol System`; Product Name is `Clock Out`.
- New feature code goes in `Assets/_Main/<Feature>/Scripts/`; packs in `Assets/Imported Assets/` (never edit).
- Gameplay assembly `ClockOut` (`Assets/Scripts/ClockOut.asmdef`), plus `ClockOut.Data` and `ClockOut.Characters`; tests in `ClockOut.PlayModeTests` (namespace `ClockOut.Tests`). New scripts outside `Assets/Scripts/` need an assembly reference decision: ask first.
- UI and feel packs (installed 2026-11-01, use them for all UI and game feel from now on, `PROCESS.md` §5.4):
  - **HKProductions Premium Casual GUI** (`Assets/Imported Assets/UI/HKProductions/`): sprites, badges, frames, buttons and the Montserrat TMP font for every HUD, screen and world-space marker. Its sprites are sliced (Multiple): load the `_0` sub-sprite.
  - **DOTween Pro** (`Assets/Plugins/Demigiant/`): every UI animation and simple tween (pop, fade, slide, punch). Use `DOTween.To` for `CanvasGroup`/`Image` fades and `SetLink(gameObject)` on every tween.
  - **Feel** (`Assets/Imported Assets/UI/Feel/`, assembly `MoreMountains.Tools`): use it where it improves the feel, for layered moments (spotted, caught, escape: freeze, camera nudge, shake, flash, sound) as `MMF_Player`s. `ClockOut` doesn't reference it yet: ask before adding the reference.
- Git: public repo https://github.com/vimeshgamedev2000-del/clock-out (`main`, LFS). Work on `feature/<short-name>` branches; never commit or push unless asked. Third-party packs (everything in `Assets/Imported Assets/` except Kenney, and `Assets/Plugins/Demigiant/`) are gitignored: never commit them (Tech Doc D-16).
- Scenes: ask before creating or editing any scene. Roles are in Tech Doc §3 (Notion).

## Game-specific rules
- Guards are driven only by `CO_EnemyAI_Script`; the other `CO_Enemy*` components own their behaviour but never choose the state.
- Presentation scripts (`CO_EnemyStateIndicator_Script`, `CO_ObjectiveHUD_Script`) read state and never write it.
- Every noise goes through `CO_NoiseManager_Script.Emit`. Exactly one guard investigates a noise, and the rest glance and carry on. Don't break this; PlayMode tests cover it.
- Run the PlayMode tests after any change to `CO_Enemy*`, `CO_NoiseManager_Script` or `CO_DistractionDevice_Script`.
