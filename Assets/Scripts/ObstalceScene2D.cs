using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ObstacleSense2D : MonoBehaviour
{
    [Header("World")]
    public LayerMask obstacleMask;

    [Header("Agent Geometry")]
    public float agentRadius    = 0.42f;   // slightly larger than your 0.4 collider
    public float probeDistance  = 1.4f;    // how far ahead we “smell”
    [Range(1, 21)] public int fanRays = 9; // center + symmetric pairs
    [Range(10f, 180f)] public float fanAngle = 90f;

    [Header("Scoring")]
    [Tooltip("How much to keep the requested heading")]
    [Range(0f, 1f)] public float keepCourseBias = 0.35f;
    [Tooltip("How much to bias toward the target direction")]
    [Range(0f, 1f)] public float targetBias = 0.40f;
    [Tooltip("Blend toward last chosen dir for stability")]
    [Range(0f, 1f)] public float smoothing = 0.20f;

    [Header("Blocking & Unstuck")]
    [Tooltip("Clearance below this is considered blocked")]
    [Range(0f, 0.5f)] public float hardBlockClearance = 0.10f;
    public float stuckSpeedThresh = 0.05f;
    public float stuckTime = 0.35f;

    // runtime
    private Rigidbody2D rb;
    private Vector2 lastBestDir = Vector2.zero;
    private float stuckTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // ==== PUBLIC HELPERS ====

    /// CircleCast LOS (corner-friendly)
    public bool HasLOS(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        var hit = Physics2D.CircleCast(from, agentRadius * 0.9f, dir.normalized, dir.magnitude, obstacleMask);
        return !hit;
    }

    /// Return [0..1] clearance for a given world-space direction
    public float Clearance01(Vector2 dir)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        var hit = Physics2D.CircleCast(rb ? rb.position : (Vector2)transform.position,
                                       agentRadius, dir, probeDistance, obstacleMask);
        if (!hit) return 1f;
        return Mathf.Clamp01(hit.distance / Mathf.Max(0.0001f, probeDistance));
    }

    /// Main: choose a velocity around obstacles.
    /// desiredDir must be normalized. Returns velocity (dir * speed).
    public Vector2 Steer(Vector2 desiredDir, float desiredSpeed, Vector2 targetPos, float dt)
    {
        Vector2 pos = rb ? rb.position : (Vector2)transform.position;
        if (desiredDir.sqrMagnitude < 0.001f || desiredSpeed <= 0f)
            return Vector2.zero;

        desiredDir = desiredDir.normalized;
        Vector2 toTarget = (targetPos - pos);
        Vector2 targetDir = (toTarget.sqrMagnitude > 0.001f) ? toTarget.normalized : desiredDir;

        // Sample a fan around desiredDir
        float half = fanAngle * 0.5f;
        Vector2 bestDir = desiredDir;
        float bestScore = float.NegativeInfinity;
        bool anyFree = false;

        int rays = Mathf.Max(1, fanRays);
        for (int i = 0; i < rays; i++)
        {
            float t = (rays == 1) ? 0.5f : (i / (float)(rays - 1));
            float ang = Mathf.Lerp(-half, +half, t);
            Vector2 dir = Rotate(desiredDir, ang);

            float clear = Clearance01(dir);         // 0..1
            if (clear > hardBlockClearance) anyFree = true;

            float score =
                clear +
                keepCourseBias * Vector2.Dot(dir, desiredDir) +
                targetBias     * Vector2.Dot(dir, targetDir);

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        // If literally everything is blocked, slide along the closest wall
        if (!anyFree)
        {
            // find the closest blocking cast (center ray)
            var centerHit = Physics2D.CircleCast(pos, agentRadius, desiredDir, probeDistance, obstacleMask);
            if (centerHit)
            {
                Vector2 n = centerHit.normal;
                Vector2 tangentA = new Vector2(-n.y, n.x);
                Vector2 tangentB = new Vector2( n.y,-n.x);
                // pick tangent closer to desiredDir
                bestDir = (Vector2.Dot(tangentA, desiredDir) > Vector2.Dot(tangentB, desiredDir)) ? tangentA : tangentB;
            }
            else
            {
                // total fallback
                bestDir = Rotate(desiredDir, 90f);
            }
        }

        // Smooth a bit for stability
        if (lastBestDir.sqrMagnitude > 0.001f)
            bestDir = Vector2.Lerp(lastBestDir, bestDir, 1f - smoothing).normalized;
        lastBestDir = bestDir;

        // Unstuck: if we aren’t moving but trying to, push a perpendicular nudge
        float speedNow = rb ? rb.linearVelocity.magnitude : 0f;
        if (speedNow < stuckSpeedThresh && desiredSpeed > 0.01f)
            stuckTimer += dt;
        else
            stuckTimer = 0f;

        if (stuckTimer > stuckTime)
        {
            // choose the clearer perpendicular
            Vector2 left  = new Vector2(-bestDir.y, bestDir.x);
            Vector2 right = new Vector2( bestDir.y,-bestDir.x);
            bestDir = (Clearance01(left) > Clearance01(right)) ? left : right;
            stuckTimer = 0f;
        }

        return bestDir * desiredSpeed;
    }

    // ==== GIZMOS ====

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Vector2 pos = rb ? rb.position : (Vector2)transform.position;
        Gizmos.DrawWireSphere(pos, agentRadius);

        // Use current velocity as reference, else up
        Vector2 refDir = (rb && rb.linearVelocity.sqrMagnitude > 0.01f) ? rb.linearVelocity.normalized : Vector2.up;

        float half = fanAngle * 0.5f;
        for (int i = 0; i < Mathf.Max(1, fanRays); i++)
        {
            float t = (fanRays == 1) ? 0.5f : (i / (float)(fanRays - 1));
            float ang = Mathf.Lerp(-half, +half, t);
            Vector2 d = Rotate(refDir, ang);
            Gizmos.DrawLine(pos, pos + d * probeDistance);
        }

        // Draw last chosen dir
        if (lastBestDir.sqrMagnitude > 0.01f)
        {
            Gizmos.color = new Color(0.1f, 1f, 0.4f, 1f);
            Gizmos.DrawLine(pos, pos + lastBestDir.normalized * (probeDistance * 0.8f));
        }
    }

    // ==== UTILS ====

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
}