using UnityEngine;
using UnityEngine.InputSystem;

namespace FormulaRacer
{
    // Local actions keep the template action asset untouched; only the new Input System is used.
    public sealed class CarInput : MonoBehaviour
    {
        public float Drive { get; private set; }
        public float Steer { get; private set; }
        private InputAction driveAction, steerAction, resetAction, cameraAction;
        private bool resetRequested;
        private bool cameraSwitchRequested;
        private float keyboardDrive, keyboardSteer, touchDrive, touchSteer;
        private bool touchDriveActive, touchSteerActive, appPaused, appUnfocused;
        private bool suspended => appPaused || appUnfocused;

        private void Awake()
        {
            driveAction = new InputAction("Drive", InputActionType.Value);
            AddAxis(driveAction, "<Keyboard>/s", "<Keyboard>/w");
            AddAxis(driveAction, "<Keyboard>/downArrow", "<Keyboard>/upArrow");
            steerAction = new InputAction("Steer", InputActionType.Value);
            AddAxis(steerAction, "<Keyboard>/a", "<Keyboard>/d");
            AddAxis(steerAction, "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            resetAction = new InputAction("Reset", InputActionType.Button, "<Keyboard>/r");
            cameraAction = new InputAction("SwitchCamera", InputActionType.Button, "<Keyboard>/c");
        }

        private static void AddAxis(InputAction action, string negative, string positive)
        {
            action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
        }

        private void OnEnable()
        {
            driveAction.Enable();
            steerAction.Enable();
            resetAction.Enable();
            cameraAction.Enable();
        }

        private void Update()
        {
            if (suspended) return;
            keyboardDrive = driveAction.ReadValue<float>();
            keyboardSteer = steerAction.ReadValue<float>();
            MergeInput();
            resetRequested |= resetAction.WasPressedThisFrame();
            cameraSwitchRequested |= cameraAction.WasPressedThisFrame();
        }

        // UI and keyboard meet here; consumers still see the same signed axes/requests.
        public void SetTouchInput(float steer, bool steerActive, float drive, bool driveActive)
        {
            if (!isActiveAndEnabled || suspended) return;
            touchSteer = Mathf.Clamp(steer, -1f, 1f);
            touchDrive = Mathf.Clamp(drive, -1f, 1f);
            touchSteerActive = steerActive;
            touchDriveActive = driveActive;
            MergeInput();
        }

        public void ClearTouchInput()
        {
            touchDrive = touchSteer = 0f;
            touchDriveActive = touchSteerActive = false;
            MergeInput();
        }

        public void RequestReset() { if (!suspended && isActiveAndEnabled) resetRequested = true; }
        public void RequestCameraSwitch() { if (!suspended && isActiveAndEnabled) cameraSwitchRequested = true; }

        private void MergeInput()
        {
            Drive = suspended ? 0f : touchDriveActive ? touchDrive : keyboardDrive;
            Steer = suspended ? 0f : touchSteerActive ? touchSteer : keyboardSteer;
        }

        private void ClearAllInput()
        {
            keyboardDrive = keyboardSteer = 0f;
            ClearTouchInput();
            resetRequested = cameraSwitchRequested = false;
            driveAction?.Reset();
            steerAction?.Reset();
            resetAction?.Reset();
            cameraAction?.Reset();
        }

        private void OnApplicationPause(bool paused) { appPaused = paused; if (paused) ClearAllInput(); }
        private void OnApplicationFocus(bool focused) { appUnfocused = !focused; if (!focused) ClearAllInput(); }

        public bool ConsumeCameraSwitch()
        {
            bool requested = cameraSwitchRequested;
            cameraSwitchRequested = false;
            return requested;
        }

        public bool ConsumeReset()
        {
            bool requested = resetRequested;
            resetRequested = false;
            return requested;
        }

        private void OnDisable()
        {
            driveAction.Disable();
            steerAction.Disable();
            resetAction.Disable();
            cameraAction.Disable();
            ClearAllInput();
        }

        private void OnDestroy()
        {
            driveAction?.Dispose();
            steerAction?.Dispose();
            resetAction?.Dispose();
            cameraAction?.Dispose();
        }
    }
}
