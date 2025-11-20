using UnityEngine;

namespace Safety
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class SimpleForwardDrive : MonoBehaviour
    {
        [Header("Motion")]
        [Tooltip("Gewenste kruissnelheid in m/s (voor safety caps).")]
        public float cruiseSpeed = 1.0f;

        [Tooltip("Respecteer de speed cap uit Robot.CurrentSpeedCap.")]
        public bool obeySafetyCap = true;

        Rigidbody rb;
        Robot gate;             // safety gate
        FieldPatrol patrol;     // wordt uitgezet

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = false;

            gate = GetComponent<Robot>();
            patrol = GetComponent<FieldPatrol>();
        }

        void OnEnable()
        {
            // FieldPatrol uitzetten zodat alleen dit script beweegt
            if (patrol != null) patrol.enabled = false;
        }

        void FixedUpdate()
        {
            // 1) Respecteer safety: Stop of EStop -> niet rijden
            if (gate != null && (gate.IsStopped || gate.IsEStopped))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                return;
            }

            // 2) Bepaal effectieve snelheid (cap meenemen)
            float v = cruiseSpeed;
            if (obeySafetyCap && gate != null && float.IsFinite(gate.CurrentSpeedCap))
                v = Mathf.Min(v, gate.CurrentSpeedCap);

            if (v <= 0.0001f) { rb.linearVelocity = Vector3.zero; return; }

            // 3) Recht vooruit bewegen langs huidige heading
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            rb.MovePosition(rb.position + fwd * v * Time.fixedDeltaTime);
        }
    }
}

