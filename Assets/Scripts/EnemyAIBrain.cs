using UnityEngine;

[RequireComponent(typeof(AITraitBuild))]
public class EnemyAIBrain : MonoBehaviour
{
    [Header("Refs (optional if autoWire = true)")]
    public EnemyControllerBasic meleeCtrl;
    public EnemyRangedShooter rangedCtrl;
    public WeaponMount meleeMount;
    public RangedMount rangedMount;

    [Header("Runtime")]
    public bool autoWire = true;
    public float applyEvery = 0.25f;
    public bool logOnStart = false;

    private AIIntentProfile intent;
    private float nextApply;

    // ---- baselines (captured once after RoundManager ramps) ----
    private float meleeBaseMoveSpeed;
    private float meleeBaseAttackCooldown;
    private float meleeBaseAttackWindup;
    private float meleeBaseStopDistance;
    private float meleeBaseAttackRange;
    private float meleeBaseApproachBuffer;

    private float rangedBaseMoveSpeed;
    private float rangedBaseShootCooldown;
    private float rangedBaseRangeSlack;
    private float rangedBaseLosCheckRadius;

    void Awake()
    {
        if (autoWire)
        {
            meleeCtrl   = meleeCtrl   ? meleeCtrl   : GetComponent<EnemyControllerBasic>();
            rangedCtrl  = rangedCtrl  ? rangedCtrl  : GetComponent<EnemyRangedShooter>();
            meleeMount  = meleeMount  ? meleeMount  : GetComponent<WeaponMount>();
            rangedMount = rangedMount ? rangedMount : GetComponent<RangedMount>();
        }
    }

    void Start()
    {
        // 0) Capture baselines AFTER RoundManager ramps
        if (meleeCtrl)
        {
            meleeBaseMoveSpeed       = meleeCtrl.moveSpeed;
            meleeBaseAttackCooldown  = meleeCtrl.attackCooldown;
            meleeBaseAttackWindup    = meleeCtrl.attackWindup;
            meleeBaseStopDistance    = meleeCtrl.stopDistance;
            meleeBaseAttackRange     = meleeCtrl.attackRange;
            meleeBaseApproachBuffer  = meleeCtrl.approachBuffer;
        }
        if (rangedCtrl)
        {
            rangedBaseMoveSpeed      = rangedCtrl.moveSpeed;
            rangedBaseShootCooldown  = rangedCtrl.shootCooldown;
            rangedBaseRangeSlack     = rangedCtrl.rangeSlack;
            rangedBaseLosCheckRadius = rangedCtrl.losCheckRadius;
        }

        // 1) Baseline desired range from weapon/shooter
        float baselineRange = 2.0f;

        if (rangedCtrl)
        {
            // Shooter exposes effective range via property (override or weapon)
            baselineRange = Mathf.Max(1f, rangedCtrl.DesiredRange);
        }
        else if (rangedMount && rangedMount.current)
        {
            baselineRange = Mathf.Max(1f, rangedMount.current.desiredRange);
        }
        else if (meleeMount && meleeMount.currentMelee)
        {
            var d = meleeMount.currentMelee;
            // derive a contact-ish distance from your melee collider
            baselineRange = Mathf.Clamp(d.colliderOffset.y + d.colliderSize.y * 0.5f + 0.5f, 1.2f, 3.5f);
        }

        // Build intent (baseline → personality → traits)
        intent = AIIntentProfile.Baseline(baselineRange);

        var build = GetComponent<AITraitBuild>();
        if (build.personality) intent.Apply(build.personality);
        if (build.traits != null)
            foreach (var t in build.traits) if (t) intent.Apply(t);

        if (logOnStart)
            Debug.Log($"{name} intent => range:{intent.desiredRange:0.0} aggr:{intent.aggression:0.00} kite:{intent.kiteBias:0.00} flank:{intent.flankAnglePref:0}");

        // 3) Apply once immediately
        ApplyToControllers();
        nextApply = Time.time + applyEvery;
    }

    void Update()
    {
        if (Time.time >= nextApply)
        {
            ApplyToControllers();
            nextApply = Time.time + applyEvery;
        }
    }

    private void ApplyToControllers()
    {
        // ----- Ranged -----
        if (rangedCtrl)
        {
            rangedCtrl.DesiredRange = intent.desiredRange; // absolute from intent

            // rangeSlack: clamp reasonable values
            rangedCtrl.rangeSlack = Mathf.Clamp(intent.rangeSlack, 0.7f, 0.98f);

            rangedCtrl.shootCooldown = Mathf.Clamp(
                rangedBaseShootCooldown / Mathf.Max(0.6f, intent.attackCadenceMult),
                0.08f, 10f
            );
            rangedCtrl.moveSpeed = Mathf.Clamp(
                rangedBaseMoveSpeed * Mathf.Lerp(0.95f, 1.10f, intent.aggression),
                0.5f, 20f
            );
            rangedCtrl.losCheckRadius = Mathf.Clamp(
                Mathf.Lerp(rangedBaseLosCheckRadius, rangedBaseLosCheckRadius + 0.2f, intent.losStrictness),
                0f, 1f
            );
        }

        // ----- Melee -----
        if (meleeCtrl)
        {
            meleeCtrl.stopDistance   = Mathf.Clamp(intent.desiredRange * 0.7f, 0.5f, 3.0f);
            meleeCtrl.attackRange    = Mathf.Clamp(intent.desiredRange,       0.8f, 3.5f);

            meleeCtrl.attackCooldown = Mathf.Clamp(
                meleeBaseAttackCooldown / Mathf.Max(0.6f, intent.attackCadenceMult),
                0.08f, 10f
            );
            meleeCtrl.approachBuffer = Mathf.Clamp(
                Mathf.Lerp(meleeBaseApproachBuffer + 0.1f, 0.06f, intent.aggression),
                0.02f, 0.4f
            );

            meleeCtrl.attackWindup = intent.earlyCommit
                ? Mathf.Clamp(meleeBaseAttackWindup * 0.7f, 0.05f, 0.5f)
                : meleeBaseAttackWindup;
        }
    }
}