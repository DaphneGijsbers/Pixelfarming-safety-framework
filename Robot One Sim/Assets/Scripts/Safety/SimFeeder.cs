using UnityEngine;
using Safety;

[RequireComponent(typeof(Rigidbody))]
public class SimFeeder : MonoBehaviour
{
    [Header("Raycast settings")]
    [SerializeField] float rayOriginHeight = 0.2f;
    [SerializeField] float rayMaxDistance = 20f;
    [SerializeField] LayerMask obstacleLayers = ~0;

    IMediator mediator;
    Rigidbody rb;
    SafetyConfig cfg;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cfg = new SafetyConfig { obstacle_stop_m = 5.0 }; // stop bij 5 m
        mediator = new Mediator(cfg);
        mediator.AttachSafetyKernel(new SafetyKernel());

        // Sluit Robot (gate + actuator) aan als port
        var robot = GetComponent<Safety.Robot>();
        if (robot != null) {
            mediator.AttachRobotPort(robot);
            Debug.Log("[SimFeeder] Attached Robot port");
        } else {
            Debug.LogWarning("[SimFeeder] No Safety.Robot component found on this GameObject.");
        }
    }

    void FixedUpdate()
    {
        var s = BuildSimSnapshot();

        // Debug input naar mediator
        // Debug.Log($"[SimFeeder] snapshot obstacle={s.nearest_obstacle_m:0.00} m");

        RobotCommand cmd = mediator.ProcessSensorData(s);

        // Debug output van mediator
        // Debug.Log($"[SimFeeder] cmd={cmd.decision} cap={cmd.speed_cap_mps:0.00} estop={cmd.estop} reasons=[{string.Join(",", cmd.reasons)}]");

        if (s.nearest_obstacle_m <= cfg.obstacle_stop_m)
            Debug.Log("Obstacle detected 5 meters");
    }

    SensorSnapshot BuildSimSnapshot()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Vector3 dir = transform.forward;

        float distance = 999f;
        if (Physics.Raycast(origin, dir, out var hit, rayMaxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            distance = hit.distance;

        return new SensorSnapshot
        {
            timestamp_s = Time.timeAsDouble,
            x = transform.position.x,
            y = transform.position.z,
            heading_rad = transform.eulerAngles.y * Mathf.Deg2Rad,
            speed_mps = rb ? rb.linearVelocity.magnitude : 0f,
            nearest_obstacle_m = distance,
            nearest_human_m = 999,
            inside_geofence = true,
            distance_to_boundary_m = 999,
            roll_deg = 0, pitch_deg = 0,
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
