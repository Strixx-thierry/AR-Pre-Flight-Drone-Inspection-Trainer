# Video Demonstration Script (target ~8 min)

Record the phone screen (Android: built-in screen recorder, or `scrcpy --record demo.mp4` over USB),
then the Unity / code walkthrough on the PC (OBS or Xbox Game Bar, Win+Alt+R). Speak over both.

---

## Part A — App on device (≈ 4 min)

| Time | On screen | Say |
|---|---|---|
| 0:00 | Tap the app icon; **Unity splash**, then the PRE·FLIGHT loading screen | "This is the Pre-Flight Drone Inspection Trainer, running on Android. It trains drone pilots to do the four-point pre-flight check from my SRS." |
| 0:20 | **Home**: point at the rail, the drone, START | "The menus are 2D, using one design system. The left rail has settings, help and records. The arrows switch aircraft." |
| 0:35 | Open **Help** | "Help gives the instructions: rotate, pan, zoom, tap parts, then launch check." |
| 0:50 | **Hangar**: tap TALON X4, then SKYFOLD F4, SELECT | "There are two aircraft with different difficulty. Each one has its own JSON checklist. Selecting returns to Home." |
| 1:05 | **Training → Briefing**: show Guided / Assessment, START | "Guided highlights each step and gives hints. Assessment hides them and scores everything." |
| 1:20 | **Inspection HUD**: drag to rotate, go underneath, pinch to zoom, two-finger pan | "The drone is a full 3D model. I can orbit all the way round and underneath, zoom in and pan, like a model viewer." |
| 1:40 | Tap **Launch Check** straight away (Assessment) or show the locked steps (Guided) | "If I try to launch now, the system refuses (FR-08). That's a safety error: you can hear the alarm and see the +1 error popup." |
| 2:00 | **Battery**: tap, show the LEDs; if low, **Swap**; slide to lock | "Faults are random every run. Here the battery is unlatched (or low charge, which I have to swap). Slide to lock: click and haptic." |
| 2:30 | **Propellers**: tap each one; the camera glides in; zoom on the tip | "The camera focuses on the part. This blade has a snapped tip and stress-whitening, so it's damaged. I mark it Damaged, then replace it." |
| 3:00 | Mark one healthy blade **Damaged** on purpose (optional) | "A false alarm counts as a minor error, so the trainee has to be accurate, not just cautious." |
| 3:15 | **Gimbal clamp**: swipe up | "The clamp must come off before power-on. It's a swipe gesture, a different interaction type." |
| 3:30 | **Calibrate RC**: centre, rotate both sticks, centre | "Controller calibration uses two virtual sticks. All eight points on each must be reached." |
| 3:50 | **Launch Check → Slide to arm** | "Everything is safe, so launch is allowed. The motors spin up with rising pitch and it lifts off." |
| 4:05 | **Report** | "The report shows the outcome, time, safety and minor errors, a per-step breakdown and the score." |
| 4:20 | **Records**, then close and **reopen the app** and show Records again | "Sessions are saved locally and persist between launches. Here are the recent runs, best time and pass rate." |
| 4:40 | Pause menu (optional) | "There's also pause, restart and quit to the hangar." |

## Part B — Unity and code walkthrough (≈ 4 min)

| Time | Open | Say |
|---|---|---|
| 4:50 | Unity **Project window**: `Assets/_Project` folders | "Code is split into Runtime and Editor assemblies. Runtime holds Core, Data, Drone, Tasks, UI, Audio and AR." |
| 5:05 | `AInspectableComponent.cs`, then `RotorModule.cs`, `BatteryLatchModule.cs` | "This abstract base is the heart of the OOP design. State is encapsulated and parts expose only safe methods. Each subclass overrides IsSafe and ApplyScenario: polymorphism." |
| 5:45 | `TaskManager.cs`: `OnPartChanged`, `RequestLaunch`, events | "Observer pattern: parts raise StateChanged, and the TaskManager re-evaluates the steps. It raises its own events that the HUD, audio and popups listen to. RequestLaunch is the launch gate." |
| 6:20 | `InspectionDirector.cs`: `PlacingState / InspectionState / EvaluationState` | "State pattern: each simulation phase has its own enter, tick and exit." |
| 6:45 | `UIFactory.cs` + `ChecklistRow.Create` | "Factory: checklist rows and UI are built from the JSON steps." |
| 7:00 | `FeedbackPopupPool.cs` | "Object pooling: popups come from a Unity ObjectPool, warmed up at start, recycled through Get and Release instead of instantiate and destroy." |
| 7:20 | **Window ▸ Asset Management ▸ Addressables ▸ Groups** (Drones group), then `DroneAssetLoader.cs` | "Addressables: the drone models live in the Drones group. The loader loads only the selected drone with LoadAssetAsync and releases it in OnDestroy." |
| 7:45 | `skyfold_f4_standard.json` (faultPool) and `SessionResult.cs` / `ProgressStore` | "Configuration is JSON in StreamingAssets, so it changes without recompiling. Sessions are saved as JSON in PlayerPrefs." |
| 8:05 | `AudioManager.cs` + `AudioLibrary` asset | "Audio is a singleton manager with a named clip library and a looping motor source." |
| 8:20 | `ARPlacementManager.cs` + `Docs/DesignChanges.md` | "AR plane detection and anchoring are implemented here. As agreed, the final build uses the 3D viewer, and the deviation is documented." |
| 8:40 | Wrap up | "That's the trainer: a meaningful inspection simulation, a clean OOP architecture, five patterns, persistence, audio and Addressables." |

### Before recording
- [ ] Rebuild Scenes, play once in the editor, Console clean.
- [ ] Build the APK and install it; play one full run so Records isn't empty.
- [ ] Phone: Do Not Disturb on, brightness up, sound on.
- [ ] Have the code files open in tabs, in the order above.
