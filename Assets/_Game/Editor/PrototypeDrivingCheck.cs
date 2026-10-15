using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace FormulaRacer.Editor
{
    // Optional integration check: real Play Mode physics with queued keyboard input.
    public static class PrototypeDrivingCheck
    {
        private const string Requested = "FormulaRacer.DrivingCheck";
        private const string Passed = "FormulaRacer.DrivingCheck.Passed";
        private static double started;
        private static ArcadeCarController car;
        private static Vector3 spawn;
        private static Quaternion rotation;
        private static bool accelerated, reversed, steered;
        private static bool cameraReturnRequested;
        private static Keyboard syntheticKeyboard;
        private static InputSettings originalSettings, batchSettings;

        [MenuItem("FormulaRacer/Milestone 1/Run Driving Smoke Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != PrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open TestTrack and exit Play Mode before running this check.");
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
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Requested, false)) return;
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling || EditorApplication.isPaused) return;
            try
            {
                if (car == null)
                {
                    car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
                    if (car == null || car.Body == null) return;
                    if (Keyboard.current == null && Application.isBatchMode)
                        syntheticKeyboard = InputSystem.AddDevice<Keyboard>("FormulaRacerSmokeKeyboard");
                    if (Keyboard.current == null) throw new InvalidOperationException("No keyboard device available.");
                    if (Application.isBatchMode)
                    {
                        originalSettings = InputSystem.settings;
                        batchSettings = UnityEngine.Object.Instantiate(originalSettings);
                        batchSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        batchSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings = batchSettings;
                        Application.runInBackground = true;
                        InputSystem.EnableDevice(Keyboard.current);
                        car.GetComponent<CarInput>().SendMessage("OnApplicationFocus", true);
                    }
                    started = Time.timeAsDouble;
                    spawn = car.Body.position;
                    rotation = car.Body.rotation;
                    accelerated = reversed = steered = false;
                    cameraReturnRequested = false;
                }
                double elapsed = Time.timeAsDouble - started;
                float speed = Vector3.Dot(car.Body.linearVelocity, car.transform.forward);
                if (elapsed < 0.5) Queue();
                // Hold C with acceleration: one press must switch once, not toggle every frame.
                else if (elapsed < 2.5) Queue(Key.W, Key.C);
                else if (elapsed < 5.5)
                {
                    if (!accelerated)
                    {
                        if (speed < 12f || Vector3.Distance(car.Body.position, spawn) < 10f)
                            throw new InvalidOperationException($"Acceleration/movement check failed: speed={speed}, distance={Vector3.Distance(car.Body.position, spawn)}, drive={car.GetComponent<CarInput>().Drive}, grounded={car.IsGrounded}.");
                        accelerated = true;
                        if (!UnityEngine.Object.FindFirstObjectByType<ChaseCamera>().IsCockpitView)
                            throw new InvalidOperationException("C did not switch to cockpit during acceleration.");
                        Debug.Log("FormulaRacer driving check: forward acceleration passed.");
                    }
                    Queue(Key.S);
                }
                else if (elapsed < 6.1)
                {
                    if (!reversed)
                    {
                        if (speed > -2f) throw new InvalidOperationException("Brake-to-reverse check failed.");
                        reversed = true;
                        Debug.Log("FormulaRacer driving check: braking and reverse passed.");
                    }
                    Queue(Key.S, Key.A);
                }
                else if (elapsed < 6.3)
                {
                    if (!steered)
                    {
                        if (Quaternion.Angle(rotation, car.Body.rotation) < 10f)
                            throw new InvalidOperationException("Steering check failed.");
                        steered = true;
                        Debug.Log("FormulaRacer driving check: steering passed.");
                    }
                    Queue(Key.R);
                }
                else if (elapsed < 6.8) Queue();
                else if (elapsed < 7.0)
                {
                    if (Vector3.Distance(car.Body.position, spawn) > 0.3f || car.Body.linearVelocity.magnitude > 0.5f || !car.IsGrounded)
                        throw new InvalidOperationException("Reset/ground stability check failed.");
                    ChaseCamera follow = UnityEngine.Object.FindFirstObjectByType<ChaseCamera>();
                    if (!cameraReturnRequested && (!follow.IsCockpitView || follow.CockpitAnchor == null ||
                        UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1 ||
                        UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1))
                        throw new InvalidOperationException("Cockpit state or single-camera/listener check failed.");
                    cameraReturnRequested = true;
                    Queue(Key.C);
                }
                else
                {
                    if (UnityEngine.Object.FindFirstObjectByType<ChaseCamera>().IsCockpitView)
                        throw new InvalidOperationException("Second C press did not restore chase view.");
                    Debug.Log("FormulaRacer driving smoke check PASSED: acceleration, braking, reverse, steering, reset, grounded stability, C switching to cockpit and back, one camera/listener in Play Mode.");
                    SessionState.SetBool(Passed, true);
                    Finish();
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish();
            }
        }

        private static void Queue(params Key[] keys)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }

        private static void Finish()
        {
            if (Keyboard.current != null) Queue();
            if (syntheticKeyboard != null && syntheticKeyboard.added) InputSystem.RemoveDevice(syntheticKeyboard);
            syntheticKeyboard = null;
            if (originalSettings != null) InputSystem.settings = originalSettings;
            if (batchSettings != null) UnityEngine.Object.DestroyImmediate(batchSettings);
            originalSettings = batchSettings = null;
            SessionState.SetBool(Requested, false);
            car = null;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Passed, false) ? 0 : 1);
        }
    }
}
