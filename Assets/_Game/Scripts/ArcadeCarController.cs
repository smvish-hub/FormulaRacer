using UnityEngine;

namespace FormulaRacer
{
    [RequireComponent(typeof(Rigidbody), typeof(CarInput))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Tooltip("Forward acceleration in metres per second squared.")]
        [SerializeField, Min(0f)] private float acceleration = 16f;
        [Tooltip("Maximum forward speed in metres per second (32 = 115 km/h).")]
        [SerializeField, Min(1f)] private float maximumSpeed = 32f;
        [Tooltip("Deceleration when the requested direction opposes motion, in m/s².")]
        [SerializeField, Min(0f)] private float braking = 30f;
        [Tooltip("Maximum reverse speed in metres per second.")]
        [SerializeField, Min(1f)] private float reverseSpeed = 7f;
        [Tooltip("Yaw degrees per second at low speed; falls with speed.")]
        [SerializeField, Min(0f)] private float steering = 100f;
        [Tooltip("Fraction of low-speed steering retained at maximum speed.")]
        [SerializeField, Range(0.1f, 1f)] private float highSpeedSteering = 0.25f;
        [Tooltip("Lateral velocity damping per second. Higher values reduce sliding.")]
        [SerializeField, Min(0f)] private float lateralGrip = 18f;
        [Tooltip("Steering input change per second; 10 reaches full steering from neutral in 0.1 seconds.")]
        [SerializeField, Min(0.1f)] private float steeringResponse = 10f;
        [Tooltip("Maximum yaw-rate change in degrees/s². Smooths direction changes without allowing spin buildup.")]
        [SerializeField, Min(1f)] private float yawAcceleration = 540f;
        [Tooltip("Cornering acceleration limit in m/s². Caps yaw rate as speed increases.")]
        [SerializeField, Min(1f)] private float corneringAcceleration = 14f;
        [Tooltip("Steering multiplier while reversing, for easier backing up.")]
        [SerializeField, Range(0.1f, 1f)] private float reverseSteering = 0.65f;
        [Tooltip("Coasting deceleration in metres per second squared.")]
        [SerializeField, Min(0f)] private float rollingResistance = 1.5f;
        [Tooltip("Ground probe length from 0.1 m above the body origin.")]
        [SerializeField, Min(0.1f)] private float groundProbeLength = 0.72f;
        [Tooltip("Track surfaces only; excludes the car and barriers.")]
        [SerializeField] private LayerMask groundMask = 1;

        public bool IsGrounded { get; private set; }
        public Rigidbody Body { get; private set; }
        private CarInput input;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float smoothedSteer;
        private float yawRate;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            input = GetComponent<CarInput>();
            spawnPosition = Body.position;
            spawnRotation = Body.rotation;
            Body.centerOfMass = new Vector3(0f, -0.28f, 0f);
        }

        private void FixedUpdate()
        {
            if (input.ConsumeReset() || Body.position.y < -10f)
            {
                ResetCar();
                return;
            }

            IsGrounded = Physics.Raycast(Body.position + Vector3.up * 0.1f,
                Vector3.down, groundProbeLength, groundMask, QueryTriggerInteraction.Ignore);
            float dt = Time.fixedDeltaTime;
            smoothedSteer = Mathf.MoveTowards(smoothedSteer, input.Steer, steeringResponse * dt);
            if (!IsGrounded)
            {
                yawRate = Mathf.MoveTowards(yawRate, 0f, yawAcceleration * dt);
                return;
            }

            Vector3 forward = Body.rotation * Vector3.forward;
            Vector3 right = Body.rotation * Vector3.right;
            Vector3 velocity = Body.linearVelocity;
            float speed = Vector3.Dot(velocity, forward);
            float target = input.Drive >= 0f ? input.Drive * maximumSpeed : input.Drive * reverseSpeed;
            bool opposing = input.Drive * speed < -0.1f;
            // Brake to zero before accelerating in the requested opposite direction.
            // Do not cross through zero at the braking rate within the same physics tick.
            if (opposing) target = 0f;
            float rate = Mathf.Abs(input.Drive) < 0.01f ? rollingResistance : opposing ? braking : acceleration;
            float nextSpeed = Mathf.MoveTowards(speed, target, rate * dt);
            float sideways = Vector3.Dot(velocity, right);
            // Preserve vertical collision/gravity response; damp only sideways motion.
            velocity += forward * (nextSpeed - speed);
            velocity -= right * sideways * (1f - Mathf.Exp(-lateralGrip * dt));
            Body.linearVelocity = velocity;

            float moving = Mathf.Clamp01(Mathf.Abs(nextSpeed) / 3f);
            float sensitivity = Mathf.Lerp(1f, highSpeedSteering, Mathf.Clamp01(Mathf.Abs(nextSpeed) / maximumSpeed));
            float desiredYawRate = smoothedSteer * steering * sensitivity * moving * Mathf.Sign(nextSpeed);
            if (nextSpeed < 0f) desiredYawRate *= reverseSteering;
            // a = speed * yawRate (radians/s): avoid tight, abrupt turns at speed.
            float yawLimit = corneringAcceleration / Mathf.Max(Mathf.Abs(nextSpeed), 3f) * Mathf.Rad2Deg;
            desiredYawRate = Mathf.Clamp(desiredYawRate, -yawLimit, yawLimit);
            yawRate = Mathf.MoveTowards(yawRate, desiredYawRate, yawAcceleration * dt);
            // Retained yaw must also respect the current speed/near-stop steering limit.
            float movingYawLimit = Mathf.Min(yawLimit, steering * sensitivity * moving * (nextSpeed < 0f ? reverseSteering : 1f));
            yawRate = Mathf.Clamp(yawRate, -movingYawLimit, movingYawLimit);
            Body.angularVelocity = Vector3.zero;
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0f, yawRate * dt, 0f));
        }

        public void ResetCar()
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = spawnPosition;
            Body.rotation = spawnRotation;
            smoothedSteer = 0f;
            yawRate = 0f;
            Body.WakeUp();
        }
    }
}
