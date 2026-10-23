# Design Changes (vs. Summative Assignment 1 proposal/SRS)

Changes made during implementation, with the reason for each. The original architecture
(State pattern, TaskManager, AInspectableComponent modules, JSON config, PlayerPrefs) is unchanged.

| # | Original (SRS) | Implemented | Reason |
|---|---|---|---|
| 1 | Training runs in **AR** on a detected real surface (FR-01, FR-02) | **Core:** a **3D inspection viewer**. **Add-on:** optional *START IN AR* mode (plane detection, raycast placement, anchoring), as recommended by the lecturer. The trainee **rotates** (drag) and **zooms** (pinch / scroll) around the drone. | Reliable demo and testing on any device and in the editor, with no dependence on lighting or ARCore support. The AR placement code (ARPlacementManager) is still in the project behind one switch: `GameSession.UseAR`. |
| 2 | One drone model | **Two drones**: SKYFOLD F4 (beginner) and TALON X4 (intermediate, larger, low-battery and rear-rotor faults) | Shows that the checklist is data-driven: same modules, a different JSON scenario per drone (NFR maintainability). |
| 3 | Generic HUD styling | **One uniform UI style** across all screens (chamfered "hangar" buttons and panels from `Assets/_Project/Art/UI`). Menus use 2D art only; the 3D model is used only in training. | Consistent look and feel; lighter menus on mobile. |
| 4 | Tracking-loss overlay (NFR reliability) | Kept. In the 3D bay it can be triggered with **T** for demonstration. | Requirement is still demonstrable without AR. |

Requirements still met: FR-03 (outline highlights), FR-04/05/06 (tap parts, latch toggle, validation),
FR-07 (HUD instructions), FR-08 (launch blocked while critical items are unsafe), FR-09/10 (timer, errors, final report).

## 5. Randomised faults per run (added)

Each run rolls its own faults from a `faultPool` in the scenario JSON, so no two inspections are the same:

| Fault | Chance (SKYFOLD / TALON) | Required action |
|---|---|---|
| Cracked/chipped propeller | 0–2 random rotors | Mark DAMAGED, then replace |
| Battery latch not secured | 80% | Lock the latch |
| Low battery (1 of 4 LEDs) | 35% / 50% | Swap the battery (latching alone does not pass) |
| Gimbal clamp still fitted | 85% | Swipe the clamp off |
| Controller calibration | always | Complete all three phases |

Rationale: the UK CAA (CAP 2534) identifies pre-flight checking and battery/power management as the leading
human-error contributors to drone equipment failures in occurrence reports, and pilot reports point to propeller
damage and seating as more frequent than motor failure. Fixed faults (`faults`) remain in the JSON for automated tests.

Sources: [UK CAA CAP 2534](https://www.caa.co.uk/our-work/publications/documents/content/cap2534) ·
[FAA: sUAS mechanical-failure statistics](https://www.faa.gov/faq/are-there-statistics-available-about-mechanical-failure-small-uas-or-drones-available-public)
