using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;          // rotate toward player (+Y forward art)
    public RangedMount mount;         // ranged mount on this enemy
    public ObstacleSense2D sense;     // obstacle-aware steering & LOS (optional)

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    [Tooltip("If > 0, overrides weapon desired range. Otherwise uses weapon’s desiredRange.")]
    public float desiredRangeOverride = -1f;
    [Tooltip("Hysteresis band. <1 means inner/outer donut around desired range.")]
    [Range(0.6f, 0.98f)] public float rangeSlack = 0.9f;

    [Header("Firing / LOS")]
    public float shootCooldown = 0.8f;  // RoundManager scales this per wave
    [Tooltip("CircleCast radius for LOS checks (helps at corners).")]
    public float losCheckRadius = 0.2f;
    [Tooltip("Walls/crates tilemap layer. Leave empty to ignore LOS.")]
    public LayerMask losMask;

    // runtime
    private Transform player;
    private Rigidbody2D rb;
    private float shootTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!mount) mount = GetComponent<RangedMount>();
        if (!sense) sense = GetComponent<ObstacleSense2D>();
        if (sense && losMask != 0) sense.obstacleMask = losMask; // share mask
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Initialize from equipped weapon if present
        if (mount && mount.current && mount.current.cooldown > 0f)
            shootCooldown = mount.current.cooldown;
    }

    private void Update()
    {
        if (!player || !mount || mount.current == null) return;

        if (shootTimer > 0f) shootTimer -= Time.deltaTime;

        // --- Aim/facing (+Y forward sprites) ---
        Vector2 toP = (player.position - transform.position);
        Vector2 aimDir = toP.sqrMagnitude > 0.0001f ? toP.normalized : Vector2.up;
        if (visual)
        {
            float z = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
            visual.rotation = Quaternion.Euler(0, 0, z);
        }

        // --- Desired range (weapon or override) ---
        float desired = (desiredRangeOverride > 0f) ? desiredRangeOverride : mount.current.desiredRange;
        float dist = toP.magnitude;
        float inner = desired * rangeSlack;   // too close -> back up
        float outer = desired / rangeSlack;   // too far  -> move in

        // --- LOS check ---
        bool hasLOS = true;
        if (losMask.value != 0)
            hasLOS = sense ? sense.HasLOS(transform.position, player.position)
                        : !Physics2D.Linecast(transform.position, player.position, losMask);

        // --- Movement decision (single 'desiredMove' for both cases) ---
        Vector2 desiredMove = Vector2.zero;

        if (!hasLOS)
        {
            // Sidestep to try to peek around cover
            Vector2 left  = new Vector2(-aimDir.y, aimDir.x);
            Vector2 right = new Vector2( aimDir.y,-aimDir.x);
            float cl = sense ? sense.Clearance01(left)  : 0.5f;
            float cr = sense ? sense.Clearance01(right) : 0.5f;
            Vector2 lateral = (cl > cr ? left : right);
            desiredMove = lateral * moveSpeed;
        }
        else
        {
            // Keep preferred range
            if      (dist < inner) desiredMove = -aimDir * moveSpeed; // back up
            else if (dist > outer) desiredMove =  aimDir * moveSpeed; // move in
            // else stay put
        }

        // --- Apply steering / velocity ---
        if (sense && desiredMove != Vector2.zero)
            rb.linearVelocity = sense.Steer(desiredMove.normalized, desiredMove.magnitude, player.position, Time.deltaTime);
        else
            rb.linearVelocity = desiredMove;

        // --- Fire when off cooldown and in LOS ---
        if (hasLOS && shootTimer <= 0f)
        {
            mount.Fire(aimDir);
            shootTimer = Mathf.Max(0.02f, shootCooldown); // never zero
        }
    }
    public float DesiredRange
    {
        get
        {
            if (desiredRangeOverride > 0f) return desiredRangeOverride;
            if (mount && mount.current && mount.current.desiredRange > 0f)
                return mount.current.desiredRange;
            return 6f; // sensible fallback
        }
        set
        {
            desiredRangeOverride = value; // let brain override
        }
    }

    void OnDrawGizmosSelected()
    {
        float desired = DesiredRange;
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, desired);

        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, desired * rangeSlack);

        Gizmos.color = new Color(0, 0.7f, 1, 0.5f);
        Gizmos.DrawWireSphere(transform.position, desired / rangeSlack);
    }



    // Optional: tidy auto-wiring when you tweak in Inspector
    void OnValidate()
    {
        if (!mount) mount = GetComponent<RangedMount>();
        if (!sense) sense = GetComponent<ObstacleSense2D>();
        if (sense && losMask != 0) sense.obstacleMask = losMask;
    }
}