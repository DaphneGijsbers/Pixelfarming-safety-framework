using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class FieldPatrol : MonoBehaviour
{
    [Header("Field (axis‑aligned)")]
    public Vector3 fieldCenter = Vector3.zero;
    public float halfWidth = 30f;
    public float halfHeight = 20f;

    [Header("Pattern")]
    public float laneSpacing = 4f;
    public bool startOnLeft = true;
    public bool firstLaneForward = true;
    public float waypointSpacing = 2.0f;

    [Header("Motion")]
    public float cruiseSpeed = 2.0f;
    public float maxYawRateDeg = 180f;
    public float baseLookAhead = 2.0f;
    public float arriveSnap = 0.2f;

    [Header("Controller gains")]
    public float kHeading = 1.0f;
    public float kHeadingD = 0.2f; 

    [Header("Gizmos/Debug")]
    public bool drawGizmos = true;
    public bool debugAngles = false;

    private List<Vector3> pts = new List<Vector3>();
    private List<float> cum = new List<float>();
    private float totalLen = 0f;
    private float sCur = 0f;
    private int segIdx = 0;

    private Rigidbody rb;
    private float prevYawErr = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        BuildPath();
        SnapToStart();
    }

    void OnValidate()
    {
        laneSpacing = Mathf.Max(0.1f, laneSpacing);
        waypointSpacing = Mathf.Max(0.2f, waypointSpacing);
        baseLookAhead = Mathf.Max(0.5f, baseLookAhead);
        maxYawRateDeg = Mathf.Clamp(maxYawRateDeg, 60f, 360f);
        cruiseSpeed = Mathf.Max(0.1f, cruiseSpeed);
        arriveSnap = Mathf.Clamp(arriveSnap, 0.05f, 1.0f);
        kHeading = Mathf.Clamp(kHeading, 0.2f, 3.0f);
        kHeadingD = Mathf.Clamp(kHeadingD, 0.0f, 2.0f);
    }

    void FixedUpdate()
    {
        if (pts.Count < 2) return;
        float dt = Time.fixedDeltaTime;

        float sProj; int segProj; Vector3 qProj;
        ProjectToPath(transform.position, out sProj, out segProj, out qProj);
        if (sProj < sCur) sProj = sCur;
        float sEndSeg = cum[Mathf.Min(segProj + 1, cum.Count - 1)];
        if (sEndSeg - sProj < arriveSnap) sProj = sEndSeg;
        sCur = Mathf.Min(sProj, totalLen);
        segIdx = Mathf.Clamp(segProj, 0, pts.Count - 2);

        float sTarget = Mathf.Min(sCur + baseLookAhead, totalLen);
        Vector3 target; int segT; Vector3 dirT;
        SampleAtArcLength(sTarget, out target, out segT, out dirT);


        Vector3 toT = target - transform.position; toT.y = 0f;
        float curYaw = transform.eulerAngles.y * Mathf.Deg2Rad;    
        float desiredYaw = Mathf.Atan2(toT.x, toT.z);               
        float yawErr = Wrap(desiredYaw - curYaw);
        float yawErrRate = (yawErr - prevYawErr) / Mathf.Max(1e-4f, dt);
        prevYawErr = yawErr;

        float yawRateCmd = kHeading * yawErr + kHeadingD * yawErrRate;
        float maxYawRate = maxYawRateDeg * Mathf.Deg2Rad;
        yawRateCmd = Mathf.Clamp(yawRateCmd, -maxYawRate, maxYawRate);

        float newYaw = curYaw + yawRateCmd * dt;
        Quaternion qNew = Quaternion.Euler(0f, newYaw * Mathf.Rad2Deg, 0f);
        rb.MoveRotation(qNew);

        float absErrDeg = Mathf.Abs(yawErr * Mathf.Rad2Deg);
        float vCmd = cruiseSpeed * Mathf.Clamp01(1.0f - absErrDeg / 180f); 
        if (absErrDeg > 100f) vCmd = 0f; 
        Vector3 fwdNew = qNew * Vector3.forward; fwdNew.y = 0f; fwdNew.Normalize();
        Vector3 newPos = transform.position + fwdNew * vCmd * dt;
        rb.MovePosition(newPos);

        float sEnd = cum[Mathf.Min(segIdx + 1, cum.Count - 1)];
        if (sCur >= sEnd) segIdx = Mathf.Min(segIdx + 1, pts.Count - 2);

        if (debugAngles)
        {
            Debug.DrawLine(transform.position + Vector3.up * 0.2f, target + Vector3.up * 0.2f, Color.magenta, 0f, false);
            Debug.Log($"seg={segIdx} yaw={curYaw*Mathf.Rad2Deg:F1} des={desiredYaw*Mathf.Rad2Deg:F1} err={yawErr*Mathf.Rad2Deg:F1} v={vCmd:F2}");
        }
    }

    void BuildPath()
    {
        pts.Clear();
        float zMin = fieldCenter.z - halfHeight, zMax = fieldCenter.z + halfHeight;
        float xMin = fieldCenter.x - halfWidth,  xMax = fieldCenter.x + halfWidth;

        int lanes = Mathf.Max(1, Mathf.FloorToInt((xMax - xMin) / laneSpacing) + 1);
        float x = startOnLeft ? xMin : xMax;
        float xStep = startOnLeft ? laneSpacing : -laneSpacing;
        bool forwardZ = firstLaneForward;

        for (int i = 0; i < lanes; i++)
        {
            AddLanePolyline(x, zMin, zMax, forwardZ);
            if (i < lanes - 1)
            {
                x += xStep; x = Mathf.Clamp(x, xMin, xMax);
                Vector3 end = pts[pts.Count - 1];
                Vector3 nextS = new Vector3(x, end.y, end.z);
                AppendIfNew(nextS);
            }
            forwardZ = !forwardZ;
        }
        Dedup(0.01f);
        BuildCumulative();
        sCur = 0f; segIdx = 0; prevYawErr = 0f;
    }

    void AddLanePolyline(float x, float zMin, float zMax, bool forward)
    {
        float len = Mathf.Abs(zMax - zMin);
        int steps = Mathf.Max(1, Mathf.FloorToInt(len / waypointSpacing));
        if (forward)
        {
            for (int s = 0; s <= steps; s++)
            {
                float z = Mathf.Lerp(zMin, zMax, s / (float)steps);
                AppendIfNew(new Vector3(x, 0f, z));
            }
        }
        else
        {
            for (int s = 0; s <= steps; s++)
            {
                float z = Mathf.Lerp(zMax, zMin, s / (float)steps);
                AppendIfNew(new Vector3(x, 0f, z));
            }
        }
    }

    void AppendIfNew(Vector3 p)
    {
        if (pts.Count == 0) { pts.Add(p); return; }
        if (DistXZ(pts[pts.Count - 1], p) > 1e-3f) pts.Add(p);
    }

    void Dedup(float eps)
    {
        if (pts.Count < 2) return;
        var cleaned = new List<Vector3>();
        cleaned.Add(pts[0]);
        for (int i = 1; i < pts.Count; i++)
            if (DistXZ(pts[i], cleaned[cleaned.Count - 1]) > eps)
                cleaned.Add(pts[i]);
        pts = cleaned;
    }

    void BuildCumulative()
    {
        cum.Clear(); cum.Add(0f);
        for (int i = 0; i < pts.Count - 1; i++)
            cum.Add(cum[cum.Count - 1] + DistXZ(pts[i], pts[i + 1]));
        totalLen = cum[cum.Count - 1];
    }

    void SnapToStart()
    {
        if (pts.Count >= 2)
        {
            transform.position = new Vector3(pts[0].x, transform.position.y, pts[0].z);
            Vector3 dir = (pts[1] - pts[0]); dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f)
                rb.MoveRotation(Quaternion.LookRotation(dir.normalized, Vector3.up));
        }
    }

    static float DistXZ(Vector3 a, Vector3 b){ return Vector2.Distance(new Vector2(a.x,a.z), new Vector2(b.x,b.z)); }
    static float Wrap(float a){ while (a >  Mathf.PI) a -= 2*Mathf.PI; while (a < -Mathf.PI) a += 2*Mathf.PI; return a; }

    void ProjectToPath(Vector3 p, out float sOut, out int segOut, out Vector3 qOut)
    {
        float bestD2 = float.MaxValue; sOut = 0f; segOut = 0; qOut = pts[0];
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            Vector3 ab = b - a; ab.y = 0f;
            Vector3 ap = p - a; ap.y = 0f;
            float ab2 = Mathf.Max(Vector3.Dot(ab, ab), 1e-6f);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab2);
            Vector3 q = a + t * ab;
            float d2 = (p - q).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2; segOut = i; qOut = q; sOut = cum[i] + t * Mathf.Sqrt(ab2);
            }
        }
    }

    void SampleAtArcLength(float s, out Vector3 point, out int segOut, out Vector3 dir)
    {
        s = Mathf.Clamp(s, 0f, totalLen);
        int i = 0; while (i < cum.Count - 1 && cum[i + 1] < s) i++;
        float segLen = Mathf.Max(cum[i + 1] - cum[i], 1e-6f);
        float t = (s - cum[i]) / segLen;
        point = Vector3.Lerp(pts[i], pts[i + 1], t);
        dir = (pts[i + 1] - pts[i]); dir.y = 0f; dir.Normalize();
        segOut = i;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(fieldCenter.x, 0f, fieldCenter.z), new Vector3(halfWidth * 2f, 0.05f, halfHeight * 2f));

        if (pts != null && pts.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < pts.Count - 1; i++) Gizmos.DrawLine(pts[i] + Vector3.up * 0.05f, pts[i + 1] + Vector3.up * 0.05f);
        }

        if (Application.isPlaying && pts != null && pts.Count > 1)
        {
            float sTarget = Mathf.Min(sCur + baseLookAhead, totalLen);
            Vector3 tPt; int sIdx; Vector3 d;
            SampleAtArcLength(sTarget, out tPt, out sIdx, out d);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(tPt + Vector3.up * 0.1f, 0.12f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, tPt + Vector3.up * 0.1f);
        }
    }
    
}
