using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace FormulaRacer
{
    public enum MobileControl { Left, Right, Accelerator, Brake, Camera, Reset }

    public sealed class PointerHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, ICancelHandler
    {
        public MobileControls Owner;
        public MobileControl Control;
        // Each UI pointer owns its lease; releasing one never releases another.
        private readonly Dictionary<int, Lease> pointers = new Dictionary<int, Lease>();
        private readonly List<int> expired = new List<int>();
        private struct Lease { public Touchscreen Screen; public int TouchId; public Mouse Mouse; }
        public bool IsHeld => pointers.Count != 0;
        public int PointerCount => pointers.Count;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || Owner == null || !Owner.CanReceiveInput ||
                pointers.ContainsKey(data.pointerId)) return;
            Lease lease = default;
            if (data is ExtendedPointerEventData extended)
            {
                lease.Screen = extended.device as Touchscreen;
                lease.TouchId = extended.touchId;
                lease.Mouse = extended.device as Mouse;
            }
            pointers.Add(data.pointerId, lease);
            Owner.Pressed(Control);
        }

        public void OnPointerUp(PointerEventData data) => ReleasePointer(data.pointerId);
        public void OnPointerExit(PointerEventData data) => ReleasePointer(data.pointerId);
        public void OnCancel(BaseEventData data)
        {
            if (data is PointerEventData pointer) ReleasePointer(pointer.pointerId);
            else ClearPointers();
        }

        public void ReleasePointer(int pointerId)
        {
            if (pointers.Remove(pointerId)) Owner?.RefreshInput();
        }

        public void ClearPointers()
        {
            pointers.Clear();
            Owner?.RefreshInput();
        }

        private void Update()
        {
            // Cancellation watchdog: also handles a device disappearing or missing pointer-up.
            expired.Clear();
            foreach (var pair in pointers)
            {
                Lease lease = pair.Value;
                if (lease.Screen != null)
                {
                    bool down = false;
                    if (lease.Screen.added)
                        foreach (var touch in lease.Screen.touches)
                            if (touch.touchId.ReadValue() == lease.TouchId && touch.press.isPressed) { down = true; break; }
                    if (!down) expired.Add(pair.Key);
                }
                else if (lease.Mouse != null && (!lease.Mouse.added || !lease.Mouse.leftButton.isPressed)) expired.Add(pair.Key);
            }
            foreach (int id in expired) ReleasePointer(id);
        }

        private void OnDisable() => ClearPointers();
        private void OnApplicationPause(bool paused) { if (paused) ClearPointers(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ClearPointers(); }
    }
}
