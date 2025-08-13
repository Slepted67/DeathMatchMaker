using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedMount : MonoBehaviour
{
    [Header("Refs")]
    public Transform visualForward;     // your Visual (+Y forward)
    public Transform rightHand;         
    public Transform leftHand;          
    public Transform firePoint;         // muzzle/spawn point
    public CombatStats stats;           // optional for attempt logging

    [Header("Equipped")]
    public WeaponRangedData current;

    // Manual override (optional). Leave null to use auto-pool:
    public ProjectilePool pool;

    // ---- runtime ----
    private GameObject visualInstance;
    private Coroutine burstCo;
    private bool burstActive;

    // ---- auto-pool state (per projectile prefab) ----
    private static Transform poolRoot;
    private static readonly Dictionary<GameObject, ProjectilePool> poolMap = new();

    // ====== PUBLIC API (mirror WeaponMount) ======

    public void Equip(WeaponRangedData data)
    {
        current = data;
        if (!current) { CleanupVisual(); return; }
        SpawnVisual();
    }

    /// <summary>Call this from input/AI with a normalized world direction.</summary>
    public void Fire(Vector2 dir)
    {
        if (!current || !firePoint || !current.projectilePrefab) return;
        stats?.RegisterAttackAttempt();

        switch (current.pattern)
        {
            case FirePattern.Single:
                SpawnProjectile(dir);
                break;

            case FirePattern.TripleSpread:
                {
                    float h = current.spreadDeg * 0.5f;
                    SpawnProjectile(Rotate(dir, -h));
                    SpawnProjectile(dir);
                    SpawnProjectile(Rotate(dir, h));
                }
                break;

            case FirePattern.RapidSmall:
                if (!burstActive) burstCo = StartCoroutine(Burst(dir, 4, current.burstInterval, jitter: true));
                break;

            case FirePattern.BurstN:
                if (!burstActive) burstCo = StartCoroutine(Burst(dir, Mathf.Max(2, current.burstCount), current.burstInterval, jitter: true));
                break;
        }
    }

    // ====== INTERNAL ======

    private void SpawnVisual()
    {
        CleanupVisual();

        if (!current.visualPrefab)
        {
            Debug.LogWarning("RangedMount: visualPrefab is null on the equipped ranged weapon.");
            return;
        }

        // Prefer hand, fallback to Visual, then self
        Transform parent = leftHand ? leftHand : (visualForward ? visualForward : transform);
        visualInstance = Instantiate(current.visualPrefab, parent);

        var t = visualInstance.transform;
        t.localPosition = new Vector3(current.gripLocalPos.x, current.gripLocalPos.y, 0f);
        t.localRotation = Quaternion.Euler(0, 0, current.gripLocalRotDeg);
        t.localScale = current.visualLocalScale;

        // Sorting to render above hands/body
        var refSR = parent.GetComponentInChildren<SpriteRenderer>();
        var sr = visualInstance.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            if (refSR)
            {
                sr.sortingLayerID = refSR.sortingLayerID;
                sr.sortingOrder = refSR.sortingOrder + 5;
            }
            else
            {
                sr.sortingOrder = 10;
            }
            sr.enabled = true;
        }
    }

    private IEnumerator Burst(Vector2 dir, int count, float gap, bool jitter)
    {
        burstActive = true;
        for (int i = 0; i < count; i++)
        {
            var shotDir = dir;
            if (jitter && current.spreadDeg > 0f)
            {
                float j = Random.Range(-current.spreadDeg, current.spreadDeg);
                shotDir = Rotate(dir, j);
            }
            SpawnProjectile(shotDir);
            if (i < count - 1) yield return new WaitForSeconds(gap);
        }
        burstActive = false;
        burstCo = null;
    }

    private int MaskForOwner(GameObject who)
    {
        var ownerLayerName = LayerMask.LayerToName(who.layer);
        if (ownerLayerName == "Player") return LayerMask.GetMask("Enemy");
        if (ownerLayerName == "Enemy")  return LayerMask.GetMask("Player");
        return ~0; // fallback: hit everything
    }

    private void SpawnProjectile(Vector2 dir)
    {
        var usePool = pool ? pool : GetOrCreatePoolFor(current.projectilePrefab);
        GameObject go = usePool
            ? usePool.Spawn(firePoint.position, Quaternion.identity)
            : Instantiate(current.projectilePrefab, firePoint.position, Quaternion.identity);

        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (go) go.transform.rotation = Quaternion.Euler(0, 0, z);

        if (go && go.TryGetComponent<DamageDealer>(out var dd))
        {
            dd.owner     = this.gameObject;
            dd.damage    = current.projectileDamage;
            dd.hitLayers = MaskForOwner(this.gameObject); // <-- important
        }

        if (go && go.TryGetComponent<Projectile>(out var p))
        {
            p.pool     = usePool;
            p.lifeTime = current.projectileLife;
            p.speed    = current.projectileSpeed;
            p.Fire(dir);
        }
    }


    private ProjectilePool GetOrCreatePoolFor(GameObject projectilePrefab)
    {
        if (!projectilePrefab) return null;

        if (poolMap.TryGetValue(projectilePrefab, out var existing) && existing)
            return existing;

        if (!poolRoot)
        {
            var pr = GameObject.Find("Pools");
            if (!pr) pr = new GameObject("Pools");
            poolRoot = pr.transform;
        }

        var go = new GameObject($"Pool_{projectilePrefab.name}");
        go.transform.SetParent(poolRoot);

        var newPool = go.AddComponent<ProjectilePool>();
        newPool.SetPrefab(projectilePrefab);   // <-- assign prefab BEFORE prewarm
        newPool.preload = 24;
        newPool.Prewarm(newPool.preload);      // <-- safe now

        poolMap[projectilePrefab] = newPool;
        return newPool;
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    private void CleanupVisual()
    {
        if (visualInstance) Destroy(visualInstance);
        visualInstance = null;
    }

    private void OnDisable()
    {
        if (burstCo != null) { StopCoroutine(burstCo); burstCo = null; burstActive = false; }
    }
}