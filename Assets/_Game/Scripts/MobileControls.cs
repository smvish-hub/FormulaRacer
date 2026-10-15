using UnityEngine;

namespace FormulaRacer
{
    [ExecuteAlways, RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class MobileControls : MonoBehaviour
    {
        public CarInput Input;
        public PointerHoldButton[] Controls;
        [Tooltip("Show touch controls in the Editor for mouse/simulated-touch tests. Desktop players remain keyboard-only.")]
        public bool ShowInEditor = true;
        [Tooltip("Steering/pedal size in Canvas reference pixels.")]
        public Vector2 ButtonSize = new Vector2(150f, 150f);
        [Tooltip("Camera/reset size in Canvas reference pixels.")]
        public Vector2 ActionButtonSize = new Vector2(88f, 88f);
        [Min(0f), Tooltip("Gap between controls in reference pixels.")]
        public float Spacing = 24f;
        [Min(0f), Tooltip("Inset from safe-area edges in reference pixels.")]
        public float EdgePadding = 32f;
        [Range(0.1f, 1f), Tooltip("Overlay opacity.")]
        public float Opacity = 0.7f;
        private bool visible, paused, unfocused;
        public bool CanReceiveInput => Application.isPlaying && isActiveAndEnabled && visible && !paused && !unfocused;

        private void OnEnable() => ApplyPresentation();
        private void Update() => ApplyPresentation();
        private void OnValidate()
        {
            ButtonSize = new Vector2(Mathf.Max(64f, ButtonSize.x), Mathf.Max(64f, ButtonSize.y));
            ActionButtonSize = new Vector2(Mathf.Max(48f, ActionButtonSize.x), Mathf.Max(48f, ActionButtonSize.y));
        }

        private void ApplyPresentation()
        {
            bool show = Application.platform == RuntimePlatform.Android || (Application.isEditor && ShowInEditor);
            if (visible && !show) ClearPointers();
            visible = show;
            Canvas canvas = GetComponent<Canvas>();
            CanvasGroup group = GetComponent<CanvasGroup>();
            canvas.enabled = show;
            group.alpha = show ? Opacity : 0f;
            group.interactable = group.blocksRaycasts = show && !paused && !unfocused;
            if (Controls == null) return;
            foreach (var control in Controls)
            {
                if (control == null) continue;
                var rect = (RectTransform)control.transform;
                bool action = control.Control == MobileControl.Camera || control.Control == MobileControl.Reset;
                bool right = control.Control == MobileControl.Accelerator || control.Control == MobileControl.Brake || action;
                Vector2 anchor = new Vector2(right ? 1f : 0f, action ? 1f : 0f);
                rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
                rect.sizeDelta = action ? ActionButtonSize : ButtonSize;
                float index = control.Control == MobileControl.Right || control.Control == MobileControl.Brake || control.Control == MobileControl.Reset ? 1f : 0f;
                float x = EdgePadding + index * ((action ? ActionButtonSize.x : ButtonSize.x) + Spacing);
                rect.anchoredPosition = new Vector2(right ? -x : x, action ? -EdgePadding : EdgePadding);
            }
        }

        public void Pressed(MobileControl control)
        {
            if (!CanReceiveInput || Input == null) return;
            if (control == MobileControl.Camera) Input.RequestCameraSwitch();
            if (control == MobileControl.Reset) Input.RequestReset();
            RefreshInput();
        }

        public void RefreshInput()
        {
            if (!Application.isPlaying || Input == null) return;
            if (!CanReceiveInput) { Input.ClearTouchInput(); return; }
            bool left = false, right = false, gas = false, brake = false;
            foreach (var control in Controls)
            {
                if (control == null || !control.isActiveAndEnabled || !control.IsHeld) continue;
                switch (control.Control)
                {
                    case MobileControl.Left: left = true; break;
                    case MobileControl.Right: right = true; break;
                    case MobileControl.Accelerator: gas = true; break;
                    case MobileControl.Brake: brake = true; break;
                }
            }
            Input.SetTouchInput((right ? 1f : 0f) - (left ? 1f : 0f), left || right,
                brake ? -1f : gas ? 1f : 0f, gas || brake);
        }

        public void ClearPointers()
        {
            if (Controls != null) foreach (var control in Controls) if (control != null) control.ClearPointers();
            if (Application.isPlaying && Input != null) Input.ClearTouchInput();
        }

        private void OnDisable() { ClearPointers(); GetComponent<Canvas>().enabled = false; }
        private void OnApplicationPause(bool value) { paused = value; if (value) ClearPointers(); }
        private void OnApplicationFocus(bool value) { unfocused = !value; if (!value) ClearPointers(); }
    }
}
