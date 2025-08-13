using UnityEngine;

public class PoolsBootstrap : MonoBehaviour
{
    public WeaponRangedData[] warmWeapons; // drag your ranged SOs here (Arrow/Orb/Javelin variants)

    void Start()
    {
        // Force-create pools for listed weapons
        foreach (var w in warmWeapons)
        {
            if (!w || !w.projectilePrefab) continue;
            var go = new GameObject($"Prewarm_{w.projectilePrefab.name}");
            var rm = go.AddComponent<RangedMount>();            // temp mount to touch the pooler API
            rm.Equip(w);
            // spawn once (then despawn immediately) to allocate a few in the pool
            var dummyDir = Vector2.up;
            rm.Fire(dummyDir);
            Destroy(go);
        }
    }
}