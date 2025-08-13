using System.Collections.Generic;
using UnityEngine;

public class MeleeOverlapScanner : MonoBehaviour
{
    [Header("Shape (template only)")]
    public BoxCollider2D shape;        // Reference collider: we read size/offset; we do NOT move it
    public Transform forwardRef;       // Usually your Visual transform (+Y is "forward")
    public LayerMask targets;          // Who can be hit (Enemy for player, Player for enemy)
    public GameObject owner;           // For crediting hits (CombatStats)

    [Header("Damage/Timing")]
    public float damage = 10f;
    public float swingDuration = 0.22f;

    [Header("Sweep Arc (single source of truth)")]
    [Range(0f, 180f)] public float sweepArcDeg = 90f; // Total arc you swing through (centered on forward)
    public int sweepSamples = 12;                      // Coverage over the whole swing (more = denser)
    public float originOffset = 0.25f;                // Push sample origin forward along +Y from forwardRef

    [Header("Debug")]
    public bool drawDebug = true;                     // Master toggle
    public Color swingColor = Color.red;              // Color for per-frame swept boxes
    public Color idleColor  = Color.gray;             // Color for editor preview when selected

    // Runtime
    private float swingT = -1f;                       // -1 = idle; otherwise elapsed time (0..swingDuration)
    private readonly HashSet<Collider2D> hitThisSwing = new();
    private ContactFilter2D filter;
    private readonly Collider2D[] buffer = new Collider2D[32];

    void Awake()
    {
        filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = targets,
            useTriggers = true
        };
    }

    /// <summary>Begin a swing; damage will be applied over time as the arc sweeps.</summary>
    public void StartSwing()
    {
        if (!shape || !forwardRef) return;
        swingT = 0f;
        hitThisSwing.Clear();
        Physics2D.SyncTransforms();
    }

    /// <summary>Immediately stop the current swing (optional helper).</summary>
    public void CancelSwing()
    {
        swingT = -1f;
        hitThisSwing.Clear();
    }

    void FixedUpdate()
    {
        if (swingT < 0f) return;
        if (!shape || !forwardRef) { swingT = -1f; return; }

        float prevT = swingT;
        swingT += Time.fixedDeltaTime;

        float pPrev = Mathf.Clamp01(prevT / swingDuration);
        float p     = Mathf.Clamp01(swingT / swingDuration);

        // Sweep from -halfArc to +halfArc across the duration
        float half = sweepArcDeg * 0.5f;

        // Sub-sample between previous and current progress to avoid gaps at low FPS
        int sub = Mathf.Max(1, Mathf.CeilToInt(sweepSamples * (p - pPrev)));
        for (int i = 1; i <= sub; i++)
        {
            float subP = Mathf.Lerp(pPrev, p, i / (float)sub);
            float localAngle = Mathf.Lerp(-half, +half, subP); // degrees around forward
            DoOverlapAt(localAngle);
        }

        if (p >= 1f) swingT = -1f; // done
    }

    private void DoOverlapAt(float localAngleDeg)
    {
        // Base forward (+Y) and its angle (convert from +X-zero to +Y-zero)
        Vector2 fwd = forwardRef.up;
        float baseAngle = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg - 90f;

        // Origin pushed forward, then add rotated local offset from the template collider
        Vector2 origin = (Vector2)forwardRef.position + fwd * originOffset;
        Quaternion rot = Quaternion.Euler(0, 0, baseAngle + localAngleDeg);

        Vector2 size     = shape.size;
        Vector2 offLocal = shape.offset;
        Vector3 offW3    = rot * new Vector3(offLocal.x, offLocal.y, 0f);
        Vector2 offWorld = new Vector2(offW3.x, offW3.y);

        Vector2 center = origin + offWorld;

        // Overlap (angle in degrees)
        int count = Physics2D.OverlapBox(center, size, baseAngle + localAngleDeg, filter, buffer);

        // Gate by the same arc (half-angle)
        float halfGate = sweepArcDeg * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var c = buffer[i];
            if (!c || hitThisSwing.Contains(c)) continue;

            // Angle gate relative to forwardRef
            Vector2 to = (Vector2)c.bounds.center - (Vector2)forwardRef.position;
            if (Vector2.Angle(fwd, to) > halfGate) continue;

            // Apply damage
            var h = c.GetComponent<Health>() ?? c.GetComponentInParent<Health>();
            if (!h) continue;

            h.TakeDamage(damage);
            hitThisSwing.Add(c);

            // Owner hit stats
            if (owner && owner.TryGetComponent<CombatStats>(out var stats))
                stats.RegisterHit();

            if (drawDebug)
                Debug.DrawLine(center, c.bounds.center, Color.green, 0.08f);
        }

        // Visualize the sampled box for a short time
        if (drawDebug)
        {
            DrawDebugBox(center, size, baseAngle + localAngleDeg, swingColor, 0.05f);
        }
    }

    // -------- DEBUG DRAW HELPERS --------

    private void DrawDebugBox(Vector2 center, Vector2 size, float angleDeg, Color col, float duration)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        Vector2 right = new(Mathf.Cos(r), Mathf.Sin(r));
        Vector2 up    = new(-right.y, right.x);

        Vector2 hx = right * (size.x * 0.5f);
        Vector2 hy = up    * (size.y * 0.5f);

        Vector2 p1 = center + hx + hy;
        Vector2 p2 = center - hx + hy;
        Vector2 p3 = center - hx - hy;
        Vector2 p4 = center + hx - hy;

        Debug.DrawLine(p1, p2, col, duration);
        Debug.DrawLine(p2, p3, col, duration);
        Debug.DrawLine(p3, p4, col, duration);
        Debug.DrawLine(p4, p1, col, duration);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebug || !shape || !forwardRef) return;

        // Preview the full arc as a set of boxes in the editor
        Vector2 fwd = forwardRef.up;
        float baseAngle = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg - 90f;
        Vector2 origin = (Vector2)forwardRef.position + fwd * originOffset;

        Vector2 size = shape.size;
        int steps = 8;
        float half = sweepArcDeg * 0.5f;

        Gizmos.color = idleColor;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float a = Mathf.Lerp(-half, +half, t);
            Quaternion rot = Quaternion.Euler(0, 0, baseAngle + a);

            Vector2 offLocal = shape.offset;
            Vector3 offW3    = rot * new Vector3(offLocal.x, offLocal.y, 0f);
            Vector2 center   = origin + new Vector2(offW3.x, offW3.y);

            DrawGizmoBox(center, size, baseAngle + a);
        }
    }

    private void DrawGizmoBox(Vector2 center, Vector2 size, float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        Vector2 right = new(Mathf.Cos(r), Mathf.Sin(r));
        Vector2 up    = new(-right.y, right.x);

        Vector2 hx = right * (size.x * 0.5f);
        Vector2 hy = up    * (size.y * 0.5f);

        Vector2 p1 = center + hx + hy;
        Vector2 p2 = center - hx + hy;
        Vector2 p3 = center - hx - hy;
        Vector2 p4 = center + hx - hy;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}