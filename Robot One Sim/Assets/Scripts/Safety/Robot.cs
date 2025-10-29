using UnityEngine;
using UnityEngine.AI;

namespace Safety
{
    /// <summary>
    /// Robot-‘actuator’ + Safety-gate in één component.
    /// - Ontvangt RobotCommand via Mediator (IRobotPort.Apply).
    /// - Houdt Stop/EStop gelatcht en een CurrentSpeedCap bij.
    /// - FieldPatrol leest deze flags/cap en beslist of/hoe er bewogen mag worden.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Robot : MonoBehaviour, IRobotPort
    {
        [Header("Optioneel (auto-gevonden)")]
        [SerializeField] Rigidbody rb;
        [SerializeField] NavMeshAgent agent;

        [Header("Debug")]
        [SerializeField] bool logDebug = true;

        // ===== Safety Gate state (leesbaar voor FieldPatrol) =====
        public bool IsStopped  { get; private set; }    // Safe Stop latch
        public bool IsEStopped { get; private set; }    // Emergency Stop latch
        public float CurrentSpeedCap { get; private set; } = float.PositiveInfinity;

        float? defaultAgentSpeed;

        void Reset()
        {
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
        }

        void Awake()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
            if (agent) defaultAgentSpeed = agent.speed;
        }

        // ===== IRobotPort: wordt door Mediator aangeroepen =====
        public void Apply(RobotCommand cmd)
        {
            if (cmd == null) return;

            // E-STOP
            if (cmd.estop || cmd.decision == Decision.EStop)
            {
                IsEStopped = true;
                IsStopped  = true;
                CurrentSpeedCap = 0f;
                if (logDebug) Debug.Log($"[Robot] E-STOP reasons=[{string.Join(",", cmd.reasons)}]");
                return;
            }

            // STOP
            if (cmd.decision == Decision.Stop)
            {
                IsStopped = true;
                CurrentSpeedCap = 0f;
                if (logDebug) Debug.Log($"[Robot] STOP reasons=[{string.Join(",", cmd.reasons)}]");
                return;
            }

            // SLOWDOWN
            if (cmd.decision == Decision.SlowDown && cmd.speed_cap_mps < float.PositiveInfinity)
            {
                IsEStopped = false;
                IsStopped  = false;
                CurrentSpeedCap = Mathf.Max(0f, cmd.speed_cap_mps);
                if (logDebug) Debug.Log($"[Robot] SLOWDOWN cap={CurrentSpeedCap:0.00} reasons=[{string.Join(",", cmd.reasons)}]");
                return;
            }

            // ALLOW
            IsEStopped = false;
            IsStopped  = false;
            CurrentSpeedCap = float.PositiveInfinity;
            if (logDebug) Debug.Log("[Robot] ALLOW");
        }

        // Dwing stilstand af zolang gelatcht, voor het geval een ander script toch duwt
        void FixedUpdate()
        {
            if (IsEStopped)
            {
                if (agent)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }
                if (rb)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                return;
            }

            if (IsStopped)
            {
                if (agent)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                if (rb)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
            else
            {
                // Herstel agent defaults als nodig
                if (agent && defaultAgentSpeed.HasValue)
                {
                    agent.isStopped = false;
                    agent.speed = defaultAgentSpeed.Value;
                }
            }
        }
    }
}
