using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerCombat2D : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;          // rotate this to face aim
    public Transform handL;           // optional, just for visual rotation
    public Transform handR;           // optional, just for visual rotation
    public WeaponMount weaponMount;   // assign in inspector (fallback grabbed in Awake)
    public RangedMount rangedMount;   // assign in Inspector

    [Header("Attack")]
    public float attackCooldown = 0.25f;    // fallback if no weapon equipped

    [Header("Aim")]
    public bool mouseAiming = true;
    public bool rotateHandsToAim = true; // visual only
    public Camera mainCam;
    public bool forwardIsUp = true;      // your art faces +Y at 0°

    // private
    private PlayerController2D mover;
    private float cdTimer;
    private Vector2 aimDir = Vector2.up;
    private Vector2 pendingStickAim;
    private float rCd;  // ranged cooldown timer

    private void Awake()
    {
        mover = GetComponent<PlayerController2D>();
        if (!mainCam) mainCam = Camera.main;
        if (!weaponMount) weaponMount = GetComponent<WeaponMount>();
    }

    private void Update()
    {
        if (cdTimer > 0) cdTimer -= Time.deltaTime;
        if (rCd > 0f) rCd -= Time.deltaTime;


        // --- resolve aim direction ---
        if (mouseAiming && mainCam && Mouse.current != null)
        {
            Vector3 m = Mouse.current.position.ReadValue();
            Vector3 world = mainCam.ScreenToWorldPoint(m);
            Vector2 dir = (world - transform.position);
            if (dir.sqrMagnitude > 0.0001f) aimDir = dir.normalized;
        }
        else
        {
            if (pendingStickAim.sqrMagnitude > 0.1f) aimDir = pendingStickAim.normalized;
            else aimDir = mover.GetFacing();
        }

        // --- rotate body (Visual) to face aim ---
        if (visual)
        {
            float z = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            if (forwardIsUp) z -= 90f; // convert +X-zero to +Y-zero sprites
            visual.rotation = Quaternion.Euler(0, 0, z);
        }

        // --- optional: rotate hands for looks ---
        if (rotateHandsToAim)
        {
            if (forwardIsUp)
            {
                if (handL) handL.up = aimDir;
                if (handR) handR.up = aimDir;
            }
            else
            {
                if (handL) handL.right = aimDir;
                if (handR) handR.right = aimDir;
            }
        }
    }

    public void OnAttackPrimary(InputValue value)
    {
        if (!value.isPressed) return;
        TrySwing();
    }

    public void OnAttackSecondary(InputValue value)
    {
        if (!value.isPressed) return;
        TryShoot();
    }

    public void OnAim(InputValue value)
    {
        pendingStickAim = value.Get<Vector2>();
    }

    private void TrySwing()
    {
        if (cdTimer > 0) return;

        // attempt stat
        GetComponent<CombatStats>()?.RegisterAttackAttempt();

        // cooldown from equipped weapon if present
        float cd = (weaponMount && weaponMount.currentMelee)
            ? weaponMount.currentMelee.cooldown
            : attackCooldown;

        // fire the scanner (stats/timing already pushed by WeaponMount.EquipMelee)
        var scanner = GetComponent<MeleeOverlapScanner>();
        if (scanner) scanner.StartSwing();

        // optional: animate the held visual
        weaponMount?.PlaySwingVisual();

        cdTimer = cd;
    }
    private void TryShoot()
    {
        if (!rangedMount || !rangedMount.current) return;

        // use weapon-defined cooldown
        float cd = rangedMount.current.cooldown;
        if (rCd > 0f) return;

        // fire toward the same aimDir used for body rotation
        rangedMount.Fire(aimDir);
        rCd = cd;
    }

}