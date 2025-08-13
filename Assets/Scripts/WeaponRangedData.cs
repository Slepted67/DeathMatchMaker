using UnityEngine;

public enum FirePattern { Single, TripleSpread, RapidSmall, BurstN }

[CreateAssetMenu(menuName="Weapons/Ranged Weapon", fileName="NewRangedWeapon")]
public class WeaponRangedData : ScriptableObject
{
    [Header("Meta")]
    public string displayName = "Ranged";

    [Header("Held Visual")]
    public GameObject visualPrefab;                 // sprite-only “in hand” visual
    public Vector3    visualLocalScale = Vector3.one;
    public Vector2    gripLocalPos     = Vector2.zero;  // align in the hand, like melee
    public float      gripLocalRotDeg  = 0f;

    [Header("Projectile")]
    public GameObject projectilePrefab;             // PRJ_Arrow / PRJ_Orb / PRJ_Javelin
    public float projectileSpeed = 12f;
    public float projectileDamage = 8f;
    public float projectileLife = 3f;

    [Header("Fire")]
    public float cooldown = 0.6f;
    public FirePattern pattern = FirePattern.Single;
    public float spreadDeg = 8f;
    public int   burstCount = 3;
    public float burstInterval = 0.06f;

    [Header("Tactics (AI)")]
    public float desiredRange = 6.5f;
}
