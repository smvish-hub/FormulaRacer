# FormulaRacer — Milestones 1 and 2

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
avoidance, race UI, and touch controls are outside this milestone.

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

Batch generation (close this project's Editor first):

```powershell
Start-Process -FilePath 'D:/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode -nographics -quit -projectPath D:/unity-projects/FormulaRacer -executeMethod FormulaRacer.Editor.PrototypeBuilder.Rebuild -logFile D:/unity-projects/FormulaRacer/Logs/Milestone1-Generate.log' -WindowStyle Hidden -Wait
Start-Process -FilePath 'D:/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode -nographics -quit -projectPath D:/unity-projects/FormulaRacer -executeMethod FormulaRacer.Editor.PrototypeBuilder.ValidateSaved -logFile D:/unity-projects/FormulaRacer/Logs/Milestone1-Validate.log' -WindowStyle Hidden -Wait
```
