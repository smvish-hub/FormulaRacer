# FormulaRacer — Milestones 1, 2 and 3

Open `Assets/_Game/Scenes/TestTrack.unity`, enter Play Mode, and focus the Game view.

- W / Up: accelerate forward.
- S / Down: brake forward motion, then reverse; W also brakes reverse motion.
- A / D or Left / Right: steer (direction reverses while backing up).
- R: reset to the start, clearing velocity. Falling below the track also resets.
- C: toggle chase/cockpit view. Each Play session starts in chase view.

Use **FormulaRacer > Milestone 1 > Rebuild Test Track** to regenerate the scene,
FormulaCar prefab, and named prototype materials. Save/close a modified TestTrack
first. Rebuild replaces those generated assets, including manual tweaks to them;
it leaves other scenes/assets alone, restores the previous active scene when it
is a different scene (otherwise keeps the rebuilt TestTrack open), and
appends TestTrack to build scenes only when absent. Use **Open Test Track** afterward.
The **Validate Saved Test Track** command checks physics, spawn, and camera wiring.

With TestTrack open and Play Mode stopped, **Run Driving Smoke Check** enters Play
Mode, queues keyboard input, checks acceleration, braking into reverse, steering,
reset, grounded stability, and switching into cockpit and back, then exits Play Mode. Avoid pressing driving keys
during this check. Results appear in the Console. This is a brief integration
check, not a full-lap or Android device test.

Tuning is on ArcadeCarController and ChaseCamera. Values use metres, seconds, and
degrees. The car uses a low centre of mass, a single body collider, interpolated
continuous collision detection, and constrained pitch/roll for this flat circuit.
Wheels are decorative; suspension, wheel animation, camera collision
avoidance and race UI are outside these milestones.

CarInput creates/disposes local Input System actions; the existing template action
asset and Player settings are untouched. No packages are required beyond those
already in the project.

## Cockpit camera setup and tuning

**FormulaRacer > Milestone 2 > Configure Cockpit Camera** upgrades the existing
FormulaCar prefab and TestTrack without rebuilding the track or changing handling.
It adds one CockpitAnchor when absent, preserves an existing anchor's tuning, and
wires the existing ChaseCamera to it. Repeating setup does not duplicate objects.
Save/close a modified TestTrack first. Full Milestone 1 rebuilds also include the
cockpit anchor and camera reference.

The anchor defaults to car-local position (0, 0.62, -0.3), above the decorative
cockpit. Adjust its Transform to tune eye position/orientation. ChaseCamera exposes
additional cockpit offset, relative look Euler angles (default 8 degrees down),
local position smoothing and local look smoothing (both default 0.04 seconds).
Local smoothing avoids trailing behind the car through its body; the camera follows
the interpolated car pose in LateUpdate. Chase offset/aim/follow smoothing retain
their existing values. Switching cuts directly to the selected view and clears
smoothing velocity; it never enables another Camera or AudioListener. The cockpit
looks toward the car's nose, including when reversing. Reset keeps the selected view.
The Milestone 2 **Toggle Camera Preview (Play Mode)** menu provides an Editor-only
preview of the same runtime view switch.

Milestone 2 compiled and passed scene reference validation and the extended Play
Mode driving/switching check using queued Input System events. Repeating incremental
setup left both prefab and scene unchanged. Cockpit framing was visually inspected
at rest in Play Mode via the preview command. Physical C-key operation and visibility/
comfort through full-speed laps, repeated turns, collisions, and resets still need
manual testing. No Android build/device test has been performed.

## Validation performed

Compiled and generated in the existing Unity 6000.6.3f1 Editor. Repeated rebuilds
passed, including rebuilding with TestTrack as the only loaded scene. Saved-scene
validation passed. The Play Mode smoke check passed acceleration, braking,
reverse, steering, reset, and grounded stability using queued keyboard input.
The chase view was visually inspected. Full laps, collision stress testing, and
Android builds/device performance have not been tested.

## Android touch controls (Milestone 3)

Use **FormulaRacer > Milestone 3 > Configure Mobile Controls** to add the overlay
to the existing TestTrack without rebuilding its track/car. It reuses one
MobileControls prefab instance and one Input System EventSystem; repeated runs
preserve Inspector tuning and refuse to save a scene with unrelated unsaved edits.
Full Milestone 1 rebuilds include the same mobile setup. Orientation is auto-rotate
with only landscape left/right permitted; other Player settings are preserved.

- Bottom left: hold left/right arrows to steer. Holding both means neutral steering.
- Bottom right: green double-chevron accelerates; orange bars brake then reverse.
  Brake wins when both pedals are held.
- Upper right: camera icon switches chase/cockpit; circular-arrow icon resets.
- Different fingers can hold steering and pedals together; multiple fingers on
  one control remain independent. Dragging out of a control releases that pointer.
  Releasing/cancelling a pointer does not release another finger's control.

MobileControls exposes button/action sizes, spacing, safe-area edge padding,
opacity, and Show In Editor. Controls show automatically on Android; desktop
players remain keyboard-only. All icons are generated UI geometry, with no imported
image/font assets. SafeAreaPanel follows Screen.safeArea when resolution/orientation
changes. Extremely large custom button sizes can still overlap; check the target
aspect ratios after tuning.

The UI sends signed axis values and one-shot requests through CarInput. Touch
overrides keyboard only on an actively held axis; keyboard resumes after release.
Keyboard WASD/arrows/C/R remain available. Controls clear pointer leases on exit,
cancel, disable, pause, and focus loss. A touch-state watchdog handles missing
pointer-up/device removal. The car's physics and camera scripts are unchanged.
The EventSystem uses InputSystemUIInputModule with independent pointers and UI
navigation disabled so driving keys cannot accidentally select/activate controls.

Use **Play TestTrack (Editor)** for an explicit test entry point, **Validate Mobile
Controls** to inspect wiring/counts, and **Run Touch Smoke Check** for synthetic
multitouch through the actual UI input module. The existing **Run Driving Smoke
Check** remains the keyboard regression test. Neither check validates a physical
device. Pause/focus checks invoke callbacks to simulate lifecycle events.

**Build TestTrack Android APK** explicitly builds only TestTrack to
`Builds/Android/FormulaRacer-TestTrack.apk` as a development build, without changing
the global scene list/order. It requires installed Android build support and will
not install modules. SampleScene remains first in the global scene list. This
command is opt-in and is not run by setup or validation.

Batch check entry points (do not pass -quit; the checks exit with success/failure):
`FormulaRacer.Editor.PrototypeDrivingCheck.RunBatch` and
`FormulaRacer.Editor.MobileControlsCheck.RunBatch`. Both create temporary input
devices/settings as needed for a headless Editor; no persistent Input settings are
changed. Batch setup/validation methods can use -quit normally.

Milestone 3 verification completed in Unity 6000.6.3f1 headless Play Mode:

- Compilation succeeded and saved physics/camera/mobile scene validation passed.
- Keyboard regression passed using queued keyboard events (a temporary keyboard
  and transient focus settings are supplied when headless mode has no device).
- Synthetic Touchscreen events passed through InputSystemUIInputModule and actual
  UI raycasts/handlers into CarInput. Checks passed simultaneous steering/pedal,
  separate pointer-up, two fingers on one pedal, brake priority, cancellation,
  simulated pause/focus callbacks, control/overlay disable, drag-out release,
  touch-device removal, and the existing camera/reset consumers.
- Three setup runs produced identical scene/prefab bytes with one Canvas, one
  EventSystem, six controls, and unchanged global build order. Full track
  regeneration was not rerun; its mobile integration uses the same EnsureScene routine.
- The initial headless checks needed virtual-device/focus/simulation-time harness
  fixes. An unnecessary scratch-scene assertion was removed because Unity rejects
  additive scene creation beside an unsaved batch-startup scene. Final checks passed.

No Android APK or physical-device test was run. Device checks still need to cover
real multitouch, rotation/safe-area changes and cutouts, interrupted gestures,
OS background/resume, target-size comfort, and device performance. The headless
checks do not visually verify the overlay. Nothing has been staged/committed/pushed;
the existing b8df926 checkpoint remains the rollback point.

Batch generation (close this project's Editor first):

```powershell
Start-Process -FilePath 'D:/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode -nographics -quit -projectPath D:/unity-projects/FormulaRacer -executeMethod FormulaRacer.Editor.PrototypeBuilder.Rebuild -logFile D:/unity-projects/FormulaRacer/Logs/Milestone1-Generate.log' -WindowStyle Hidden -Wait
Start-Process -FilePath 'D:/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode -nographics -quit -projectPath D:/unity-projects/FormulaRacer -executeMethod FormulaRacer.Editor.PrototypeBuilder.ValidateSaved -logFile D:/unity-projects/FormulaRacer/Logs/Milestone1-Validate.log' -WindowStyle Hidden -Wait
```
