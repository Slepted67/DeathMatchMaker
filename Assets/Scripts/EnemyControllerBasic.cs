using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyControllerBasic : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;                  // rotates to face the player (+Y forward art)
    public WeaponMeleeData enemyMeleeWeapon;  // assign a melee weapon SO in Inspector
    public ObstacleSense2D sense;             // optional; set obstacleMask to Obstacles

    [Header("Movement/Attack")]
    public float moveSpeed = 5f;
    public float stopDistance = 1.7f;     // where we stop pushing forward
    public float attackRange = 2.0f;      // when we can attack
    public float approachBuffer = 0.15f;  // hysteresis to reduce edge stutter
    public float attackCooldown = 1.0f;
    public float attackWindup = 0.15f;    // delay before swing fires

    private Transform player;
    private Rigidbody2D rb;
    private float attackTimer;

    private enum State { Seek, Windup, Recover }
    private State state = State.Seek;

    // cached comps
    private MeleeOverlapScanner scanner;
    private WeaponMount mount;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        scanner = GetComponent<MeleeOverlapScanner>();
        mount   = GetComponent<WeaponMount>();
        if (!sense) sense = GetComponent<ObstacleSense2D>(); // auto-wire if present
    }

    void Start()
    {
        // find player
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // equip weapon (pushes damage/sweep/shape to scanner & spawns visual)
        if (mount && enemyMeleeWeapon) mount.EquipMelee(enemyMeleeWeapon);
    }

    void Update()
    {
        if (!player) return;

        // cooldown
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // face the player (+Y forward)
        Vector2 toPlayer = (player.position - transform.position);
        Vector2 aimDir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.up;
        if (visual)
        {
            float z = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
            visual.rotation = Quaternion.Euler(0, 0, z);
        }

        float dist = toPlayer.magnitude;

        switch (state)
        {
            case State.Seek:
                // enter windup only when clearly inside range (hysteresis)
                if (dist <= attackRange - approachBuffer && attackTimer <= 0f)
                {
                    state = State.Windup;
                    Invoke(nameof(DoAttack), attackWindup);
                }
                break;

            case State.Windup:
                // waiting for DoAttack to fire
                break;

            case State.Recover:
                if (attackTimer <= 0f) state = State.Seek;
                break;
        }
    }

    void FixedUpdate()
    {
        if (!player) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;

        if (state == State.Seek)
        {
            // hysteresis band so we don’t oscillate at the boundary
            float enterChase = stopDistance + approachBuffer;
            float enterStop  = stopDistance - approachBuffer;

            Vector2 desiredVel = Vector2.zero;
            if      (dist > enterChase) desiredVel = toPlayer.normalized * moveSpeed;
            else if (dist > enterStop ) desiredVel = toPlayer.normalized * (moveSpeed * 0.35f);

            // obstacle-aware steer if available
            if (sense && desiredVel != Vector2.zero)
                rb.linearVelocity = sense.Steer(desiredVel.normalized, desiredVel.magnitude, player.position, Time.fixedDeltaTime);
            else
                rb.linearVelocity = desiredVel;
        }
        else
        {
            // lock movement during windup/recover so attacks feel committed
            rb.linearVelocity = Vector2.zero;
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(DoAttack));
    }

    void DoAttack()
    {
        // It’s possible the object got disabled after Invoke was scheduled
        if (!isActiveAndEnabled) return;
        if (!player) return;

        GetComponent<CombatStats>()?.RegisterAttackAttempt();

        if (scanner) scanner.StartSwing();
        mount?.PlaySwingVisual();

        attackTimer = attackCooldown;
        state = State.Recover;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}