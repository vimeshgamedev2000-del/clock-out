# Clock Out

A stealth game about leaving the office on time. Sneak past coworkers who want to hand you more work, get rid of the folders you're stuck with, and reach the elevator before the clock runs out.

Portfolio project, work in progress. Built in Unity 6 (URP) for PC and WebGL.

## Opening the project

- Unity **6000.3.19f1**, Universal Render Pipeline
- Clone with [Git LFS](https://git-lfs.com) installed (textures, models, audio and fonts are stored in LFS)

The following third-party packs are **not included** in this repo because their licences don't allow redistributing the raw files. Import them into the listed folders before opening the scenes:

| Pack | Folder |
|---|---|
| DOTween Pro (Demigiant) | `Assets/Plugins/Demigiant/` |
| Feel (More Mountains) | `Assets/Imported Assets/UI/Feel/` |
| Premium Casual GUI (HKProductions) | `Assets/Imported Assets/UI/HKProductions/` |
| Low Poly Office Set (VNB) | `Assets/Imported Assets/VNB Office Set/` |
| Scalable Grid Prototype Materials | `Assets/Imported Assets/Scalable Grid Prototype Materials/` |

[Kenney Mini Characters](https://kenney.nl) (CC0) is included.

## Code layout

- `Assets/Scripts/`: gameplay assembly `ClockOut`
- `Assets/_Main/<Feature>/Scripts/`: feature code (`ClockOut.Data`, `ClockOut.Characters`)
- `Assets/Tests/PlayMode/`: PlayMode tests (`ClockOut.PlayModeTests`)
- `Docs/`: process notes
