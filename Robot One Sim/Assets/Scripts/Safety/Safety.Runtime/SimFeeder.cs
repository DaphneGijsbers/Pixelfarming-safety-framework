using UnityEngine;
using Safety;

[RequireComponent(typeof(Rigidbody))]
public class SimFeeder : MonoBehaviour
{
    [Header("Raycast (obstacle)")]
    [SerializeField] float rayOriginHeight = 0.2f;
    [SerializeField] float rayMaxDistance = 20f;
    [SerializeField] LayerMask obstacleLayers = ~0;

    [Header("Geofence")]
    [SerializeField] float geofenceBufferMeters = 5f;   // 5 m rondom je veld
    [SerializeField] bool  autoSyncFromFieldPatrol = true;

    // Afgeleide geofence (rechthoek)
    Vector3 fenceCenter;
    float   fenceHalfWidth;
    float   fenceHalfHeight;

    IMediator mediator;
    Rigidbody rb;
    SafetyConfig cfg;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Zet je thresholds; geofence_margin_m = stopdrempel op de fence
        cfg = new SafetyConfig {
            obstacle_stop_m   = 5.0,
            geofence_margin_m = 0.5        // stop wanneer binnen 0.5 m van de geofence-rand
        };

        mediator = new Mediator(cfg);
        mediator.AttachSafetyKernel(new SafetyKernel());

        var robot = GetComponent<Safety.Robot>();
        if (robot != null) mediator.AttachRobotPort(robot);

        // Sync geofence uit FieldPatrol-veld + buffer
        SyncGeofenceFromField();
    }

    void SyncGeofenceFromField()
    {
        // Probeer FieldPatrol uit te lezen (zelfde GameObject)
        var fp = GetComponent<FieldPatrol>();
        if (autoSyncFromFieldPatrol && fp != null)
        {
            fenceCenter     = fp.fieldCenter;
            fenceHalfWidth  = fp.halfWidth  + geofenceBufferMeters;
            fenceHalfHeight = fp.halfHeight + geofenceBufferMeters;
        }
        else
        {
            // fallback: als je geen FieldPatrol hebt, kies iets (hier rond (0,0))
            fenceCenter     = Vector3.zero;
            fenceHalfWidth  = 30f + geofenceBufferMeters;
            fenceHalfHeight = 20f + geofenceBufferMeters;
        }
    }

    void FixedUpdate()
    {
        var s = BuildSimSnapshot();
        mediator.ProcessSensorData(s);
    }

    SensorSnapshot BuildSimSnapshot()
    {
        
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Vector3 dir = transform.forward;
        float nearest = 999f;
        if (Physics.Raycast(origin, dir, out var hit, rayMaxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            nearest = hit.distance;

        
        var pos = transform.position;
        bool inside =
            pos.x >= fenceCenter.x - fenceHalfWidth  && pos.x <= fenceCenter.x + fenceHalfWidth &&
            pos.z >= fenceCenter.z - fenceHalfHeight && pos.z <= fenceCenter.z + fenceHalfHeight;

    
        float dx = Mathf.Min(Mathf.Abs(pos.x - (fenceCenter.x - fenceHalfWidth)),
                             Mathf.Abs((fenceCenter.x + fenceHalfWidth) - pos.x));
        float dz = Mathf.Min(Mathf.Abs(pos.z - (fenceCenter.z - fenceHalfHeight)),
                             Mathf.Abs((fenceCenter.z + fenceHalfHeight) - pos.z));
        float distToEdge = Mathf.Min(dx, dz);

        return new SensorSnapshot
        {
            timestamp_s = Time.timeAsDouble,
            x = pos.x,
            y = pos.z,
            heading_rad = transform.eulerAngles.y * Mathf.Deg2Rad,
            speed_mps = rb ? rb.linearVelocity.magnitude : 0f,

            nearest_obstacle_m = nearest,

            // ✅ geofence
            inside_geofence = inside,
            distance_to_boundary_m = distToEdge,

            // defaults
            nearest_human_m = 999,
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
    // 1) Optioneel in Edit Mode: sync met FieldPatrol (Awake is nog niet gelopen)
    if (!Application.isPlaying && autoSyncFromFieldPatrol)
    {
        var fp = GetComponent<FieldPatrol>();
        if (fp)
        {
            fenceCenter     = fp.fieldCenter;
            fenceHalfWidth  = fp.halfWidth  + geofenceBufferMeters;
            fenceHalfHeight = fp.halfHeight + geofenceBufferMeters;
        }
    }

    // 2) Fallback margin als cfg nog null is (Editor)
    float margin = cfg != null ? (float)cfg.geofence_margin_m : 2.0f;

    // 3) Teken obstacle ray
    Gizmos.color = Color.yellow;
    Vector3 o = transform.position + Vector3.up * rayOriginHeight;
    Gizmos.DrawLine(o, o + transform.forward * rayMaxDistance);

    // 4) Beetje inputsanity: niets tekenen als er nog geen maten zijn
    if (fenceHalfWidth <= 0f || fenceHalfHeight <= 0f) return;

    // 5) Buitenste geofence (groen)
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(
        new Vector3(fenceCenter.x, 0f, fenceCenter.z),
        new Vector3(fenceHalfWidth * 2f, 0.02f, fenceHalfHeight * 2f)
    );

    // 6) Stop-lijn (rood) net binnen de fence, met veilige margin fallback
    float stopW = Mathf.Max(0f, fenceHalfWidth  * 2f - 2f * margin);
    float stopH = Mathf.Max(0f, fenceHalfHeight * 2f - 2f * margin);

    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(
        new Vector3(fenceCenter.x, 0f, fenceCenter.z),
        new Vector3(stopW, 0.02f, stopH)
    );
}
}