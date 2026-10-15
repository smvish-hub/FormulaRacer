using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace FormulaRacer.Editor
{
    // Real Input System touch events traverse the UI module/raycaster/hold handlers into CarInput.
    public static class MobileControlsCheck
    {
        private const string Requested = "FormulaRacer.MobileCheck";
        private const string Passed = "FormulaRacer.MobileCheck.Passed";
        private static MobileControls ui;
        private static Touchscreen screen;
        private static ArcadeCarController car;
        private static ChaseCamera follow;
        private static Vector3 spawn;
        private static bool initialView;
        private static int step, lastFrame;
        private static double nextStep;
        private static InputSettings originalSettings, batchSettings;

        [MenuItem("FormulaRacer/Milestone 3/Run Touch Smoke Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != PrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open TestTrack and stop Play Mode first.");
            SessionState.SetBool(Requested, true);
            SessionState.SetBool(Passed, false);
            EditorApplication.isPlaying = true;
        }

        public static void RunBatch()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            Run();
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayState;
            EditorApplication.playModeStateChanged += OnPlayState;
        }

        private static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(Requested, false)) Finish();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Requested, false) || !EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling) return;
            try
            {
                if (ui == null)
                {
                    ui = UnityEngine.Object.FindFirstObjectByType<MobileControls>();
                    car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
                    follow = UnityEngine.Object.FindFirstObjectByType<ChaseCamera>();
                    if (ui == null || car == null || car.Body == null) { ui = null; return; }
                    ui.ShowInEditor = true;
                    if (Application.isBatchMode)
                    {
                        originalSettings = InputSystem.settings;
                        batchSettings = UnityEngine.Object.Instantiate(originalSettings);
                        batchSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        batchSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings = batchSettings;
                        Application.runInBackground = true;
                        ui.SendMessage("OnApplicationFocus", true);
                        ui.Input.SendMessage("OnApplicationFocus", true);
                    }
                    ui.SendMessage("Update");
                    if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    screen = InputSystem.AddDevice<Touchscreen>("FormulaRacerSmokeTouch");
                    spawn = car.Body.position;
                    initialView = follow.IsCockpitView;
                    step = 0;
                    nextStep = Time.timeAsDouble + 0.4;
                    lastFrame = Time.frameCount;
                }
                if (Time.timeAsDouble < nextStep || Time.frameCount < lastFrame + 3) return;
                nextStep = Time.timeAsDouble + 0.25;
                lastFrame = Time.frameCount;
                switch (step++)
                {
                    case 0: Touch(11, MobileControl.Left, TouchPhase.Began); Touch(12, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 1: Axes(-1, 1); Touch(11, MobileControl.Left, TouchPhase.Ended); break;
                    case 2: Axes(0, 1); Touch(13, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 3:
                        Require(Button(MobileControl.Accelerator).PointerCount == 2, "two pointers on one pedal");
                        Touch(12, MobileControl.Accelerator, TouchPhase.Ended); break;
                    case 4: Axes(0, 1); Touch(14, MobileControl.Brake, TouchPhase.Began); break;
                    case 5: Axes(0, -1); Touch(14, MobileControl.Brake, TouchPhase.Canceled); break;
                    case 6: Axes(0, 1); Touch(13, MobileControl.Accelerator, TouchPhase.Ended); break;
                    case 7: Axes(0, 0); Touch(15, MobileControl.Left, TouchPhase.Began); Touch(16, MobileControl.Right, TouchPhase.Began); break;
                    case 8: Axes(0, 0); Touch(15, MobileControl.Left, TouchPhase.Ended); break;
                    case 9: Axes(1, 0); Touch(16, MobileControl.Right, TouchPhase.Canceled); break;
                    case 10: Axes(0, 0); Touch(17, MobileControl.Left, TouchPhase.Began); Touch(18, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 11:
                        Axes(-1, 1);
                        ExecuteEvents.Execute(Button(MobileControl.Left).gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
                        break;
                    case 12:
                        Axes(0, 1);
                        ui.SendMessage("OnApplicationPause", true); ui.Input.SendMessage("OnApplicationPause", true);
                        Axes(0, 0); ClearLeases();
                        Touch(17, MobileControl.Left, TouchPhase.Canceled); Touch(18, MobileControl.Accelerator, TouchPhase.Canceled);
                        ui.SendMessage("OnApplicationPause", false); ui.Input.SendMessage("OnApplicationPause", false);
                        break;
                    case 13: Touch(19, MobileControl.Right, TouchPhase.Began); Touch(20, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 14:
                        Axes(1, 1);
                        ui.SendMessage("OnApplicationFocus", false); ui.Input.SendMessage("OnApplicationFocus", false);
                        Axes(0, 0); ClearLeases();
                        Touch(19, MobileControl.Right, TouchPhase.Canceled); Touch(20, MobileControl.Accelerator, TouchPhase.Canceled);
                        ui.SendMessage("OnApplicationFocus", true); ui.Input.SendMessage("OnApplicationFocus", true); break;
                    case 15: Touch(21, MobileControl.Left, TouchPhase.Began); Touch(22, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 16:
                        Axes(-1, 1); Button(MobileControl.Left).enabled = false; Axes(0, 1);
                        ui.enabled = false; Axes(0, 0); ClearLeases();
                        Touch(21, MobileControl.Left, TouchPhase.Canceled); Touch(22, MobileControl.Accelerator, TouchPhase.Canceled);
                        Button(MobileControl.Left).enabled = true; ui.enabled = true; break;
                    case 17: Touch(23, MobileControl.Camera, TouchPhase.Began); break;
                    case 18:
                        Require(follow.IsCockpitView != initialView, "touch camera request consumed by ChaseCamera");
                        Touch(23, MobileControl.Camera, TouchPhase.Ended);
                        car.Body.position += Vector3.up * 2f;
                        Touch(24, MobileControl.Reset, TouchPhase.Began); break;
                    case 19:
                        Require(Vector3.Distance(car.Body.position, spawn) < 0.3f && car.Body.linearVelocity.magnitude < 0.5f,
                            "touch reset consumed by ArcadeCarController");
                        Touch(24, MobileControl.Reset, TouchPhase.Ended); break;
                    case 20: Touch(25, MobileControl.Accelerator, TouchPhase.Began); break;
                    case 21:
                        Axes(0, 1);
                        InputSystem.QueueStateEvent(screen, new TouchState { touchId = 25, phase = TouchPhase.Moved,
                            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), pressure = 1f }); break;
                    case 22: Axes(0, 0); Touch(25, MobileControl.Accelerator, TouchPhase.Ended); break;
                    case 23: Touch(26, MobileControl.Right, TouchPhase.Began); break;
                    case 24: Axes(1, 0); InputSystem.RemoveDevice(screen); screen = null; break;
                    case 25: Axes(0, 0); ClearLeases(); break;
                    default:
                        Axes(0, 0); ClearLeases(); Milestone3Setup.ValidateScene(SceneManager.GetActiveScene());
                        Debug.Log("FormulaRacer touch smoke check PASSED: actual synthetic Touchscreen events through UI into CarInput; simultaneous steering/pedal, independent pointer release, two fingers on one pedal, brake priority, touch cancellation, control cancellation, simulated pause/focus callbacks, control/overlay disable, pointer exit, device removal, camera/reset consumers.");
                        SessionState.SetBool(Passed, true);
                        Finish(); break;
                }
            }
            catch (Exception error) { Debug.LogException(error); Finish(); }
        }

        private static PointerHoldButton Button(MobileControl control) => ui.Controls.Single(b => b.Control == control);
        private static void Touch(int id, MobileControl control, TouchPhase phase)
        {
            var rect = (RectTransform)Button(control).transform;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, phase = phase, position = position, pressure = 1f });
        }
        private static void Axes(float steer, float drive)
        {
            Require(Mathf.Abs(ui.Input.Steer - steer) < 0.01f && Mathf.Abs(ui.Input.Drive - drive) < 0.01f,
                $"stage {step}: expected steer/drive {steer}/{drive}, got {ui.Input.Steer}/{ui.Input.Drive}");
        }
        private static void ClearLeases() => Require(ui.Controls.All(b => b.PointerCount == 0), "all pointer leases cleared");
        private static void Require(bool passed, string check) { if (!passed) throw new InvalidOperationException("Mobile check failed: " + check); }
        private static void Finish()
        {
            SessionState.SetBool(Requested, false);
            if (ui != null) ui.ClearPointers();
            if (screen != null && screen.added) InputSystem.RemoveDevice(screen);
            screen = null; ui = null;
            if (originalSettings != null) InputSystem.settings = originalSettings;
            if (batchSettings != null) UnityEngine.Object.DestroyImmediate(batchSettings);
            originalSettings = batchSettings = null;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Passed, false) ? 0 : 1);
        }
    }
}
