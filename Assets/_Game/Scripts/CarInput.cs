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
            Drive = driveAction.ReadValue<float>();
            Steer = steerAction.ReadValue<float>();
            resetRequested |= resetAction.WasPressedThisFrame();
            cameraSwitchRequested |= cameraAction.WasPressedThisFrame();
        }

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
            Drive = Steer = 0f;
            resetRequested = false;
            cameraSwitchRequested = false;
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
