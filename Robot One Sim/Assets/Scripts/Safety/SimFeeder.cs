using UnityEngine;
using Safety;

public class SimFeeder : MonoBehaviour
{
    [Header("Raycast settings")]
        [SerializeField] float rayOriginHeight = 0.2f;   // beetje boven de grond
        [SerializeField] float rayMaxDistance = 20f;     // hoe ver vooruit kijken
        [SerializeField] LayerMask obstacleLayers = ~0;

    IMediator mediator;
    Rigidbody rb;
    SafetyConfig cfg;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cfg = new SafetyConfig();
        mediator = new Mediator(cfg);
        mediator.AttachSafetyKernel(new SafetyKernel());
    }

    void FixedUpdate()
    {
        var s = BuildSimSnapshot();
        RobotCommand cmd = mediator.ProcessSensorData(s);

        if (s.nearest_obstacle_m <= cfg.obstacle_stop_m)
            {
                // Exacte tekst:
                //Debug.Log("Obstacle detected 5 meters");
                // Of met gemeten waarde:
                // Debug.Log($"Obstacle detected {snap.nearest_obstacle_m:0.0} meters");
            }
    }

    SensorSnapshot BuildSimSnapshot()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Vector3 dir = transform.forward;

        float distance = 999f;
        if (Physics.Raycast(origin, dir, out var hit, rayMaxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            distance = hit.distance;
        }

        // Bouw snapshot
        return new SensorSnapshot
        {
            timestamp_s = Time.timeAsDouble,
            x = transform.position.x,
            y = transform.position.z,
            heading_rad = transform.eulerAngles.y * Mathf.Deg2Rad,
            speed_mps = rb ? rb.linearVelocity.magnitude : 0f,

            nearest_obstacle_m = distance,

            // Defaults; pas aan als je meer simulatiebronnen toevoegt:
            nearest_human_m = 999,
            inside_geofence = true,
            distance_to_boundary_m = 999,
            roll_deg = 0,
            pitch_deg = 0,
            stability_warning = false,
            tool_requested_active = false,
            tool_hw_interlock_ok = true,
            link_ok = true,
            estop_pressed = false
        };
    }
    void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
            Gizmos.DrawLine(origin, origin + transform.forward * rayMaxDistance);
        }
}
