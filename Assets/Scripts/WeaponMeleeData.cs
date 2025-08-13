using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Melee Weapon", fileName = "NewMeleeWeapon")]
public class WeaponMeleeData : ScriptableObject
{
    [Header("Meta")]
    public string displayName = "Melee";
    public GameObject visualPrefab;
    public Vector3 visualLocalScale = Vector3.one;
    public bool twoHanded = false;

    [Header("Damage/Timing")]
    public float damage = 10f;
    public float swingDuration = 0.22f;
    public float cooldown = 0.30f;

    [Header("Sweep Arc (single source of truth)")]
    [Range(0f, 180f)] public float sweepArcDeg = 90f; // total arc you swing through
    public int sweepSamples = 12;                      // coverage; higher = denser
    public float originOffset = 0.25f;                // push origin forward along +Y

    [Header("Hitbox Shape (scanner reads this)")]
    public Vector2 colliderSize = new(0.7f, 1.1f);
    public Vector2 colliderOffset = new(0f, 0.65f);

    [Header("Grip (visual only)")]
    public Vector2 gripLocalPos = Vector2.zero;
    public float  gripLocalRotDeg = 0f;
}
