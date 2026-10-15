using UnityEngine;

namespace FormulaRacer
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target;
        [Tooltip("Car-local eye anchor. Position/rotate this transform to adjust the cockpit view.")]
        public Transform CockpitAnchor;
        [Tooltip("Camera position relative to the car: behind and above.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -7f);
        [Tooltip("Height of the point the camera aims at above the car.")]
        [SerializeField] private float lookHeight = 0.7f;
        [Tooltip("Position smoothing time in seconds.")]
        [SerializeField, Min(0.01f)] private float smoothTime = 0.16f;
        [Tooltip("Additional eye position relative to the cockpit anchor, in metres.")]
        [SerializeField] private Vector3 cockpitOffset = Vector3.zero;
        [Tooltip("Look direction relative to the cockpit anchor, in Euler degrees (positive X looks down).")]
        [SerializeField] private Vector3 cockpitLookEuler = new Vector3(8f, 0f, 0f);
        [Tooltip("Local eye-position smoothing in seconds. Zero locks directly to the anchor; no world-space follow lag.")]
        [SerializeField, Min(0f)] private float cockpitSmoothTime = 0.04f;
        [Tooltip("Local look-direction smoothing time in seconds. Zero follows directly.")]
        [SerializeField, Min(0f)] private float cockpitLookSmoothTime = 0.04f;
        public bool IsCockpitView { get; private set; }
        private Vector3 velocity;
        private Vector3 localEye, localEyeVelocity;
        private Quaternion localLook;
        private Transform boundTarget;
        private CarInput input;

        public bool SetCockpitView(bool cockpit)
        {
            if (Target == null || (cockpit && CockpitAnchor == null)) return false;
            IsCockpitView = cockpit;
            velocity = localEyeVelocity = Vector3.zero;
            localEye = cockpitOffset;
            localLook = Quaternion.Euler(cockpitLookEuler);
            // Cut directly between views instead of sweeping the camera through the car.
            if (cockpit) ApplyCockpitPose();
            else
            {
                transform.position = Target.TransformPoint(offset);
                transform.LookAt(Target.position + Vector3.up * lookHeight, Vector3.up);
            }
            return true;
        }

        private void ApplyCockpitPose()
        {
            transform.SetPositionAndRotation(CockpitAnchor.TransformPoint(localEye), CockpitAnchor.rotation * localLook);
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (boundTarget != Target)
            {
                boundTarget = Target;
                input = Target.GetComponent<CarInput>();
            }
            if (input != null && input.ConsumeCameraSwitch()) SetCockpitView(!IsCockpitView);
            if (IsCockpitView && CockpitAnchor != null)
            {
                localEye = cockpitSmoothTime <= 0f ? cockpitOffset : Vector3.SmoothDamp(
                    localEye, cockpitOffset, ref localEyeVelocity, cockpitSmoothTime);
                Quaternion desiredLook = Quaternion.Euler(cockpitLookEuler);
                float blend = cockpitLookSmoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / cockpitLookSmoothTime);
                localLook = Quaternion.Slerp(localLook, desiredLook, blend);
                ApplyCockpitPose();
                return;
            }
            if (IsCockpitView) SetCockpitView(false);
            Vector3 desired = Target.TransformPoint(offset);
            // Teleports/reset should not drag the camera across the whole circuit.
            if ((transform.position - desired).sqrMagnitude > 400f)
            {
                transform.position = desired;
                velocity = Vector3.zero;
            }
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            Vector3 aim = Target.position + Vector3.up * lookHeight - transform.position;
            if (aim.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
        }
    }
}
