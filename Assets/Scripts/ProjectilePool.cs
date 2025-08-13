using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [Header("Config")]
    public GameObject prefab;
    public int preload = 16;

    private readonly Queue<GameObject> pool = new();
    private bool prewarmed;

    void Start()
    {
        if (prefab && !prewarmed) Prewarm(preload);
    }

    public void SetPrefab(GameObject p) => prefab = p;

    public void Prewarm(int count)
    {
        if (!prefab) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
        prewarmed = true;
    }

    public GameObject Spawn(Vector3 pos, Quaternion rot)
    {
        if (!prefab)
        {
            Debug.LogError("ProjectilePool.Spawn: prefab is null.");
            return null;
        }

        GameObject go = null;

        // Drain out destroyed/null entries safely
        while (pool.Count > 0 && go == null)
        {
            go = pool.Dequeue();
            if (go == null) continue;                 // destroyed externally
            if (!go) { go = null; continue; }         // MissingReference
        }

        if (go == null)
            go = Instantiate(prefab, transform);

        // Recheck in case something odd happened
        if (!go)
        {
            Debug.LogError("ProjectilePool.Spawn: failed to get or create projectile.");
            return null;
        }

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Despawn(GameObject go)
    {
        if (!go) return;                 // ignore double-despawn/destroyed
        go.SetActive(false);
        // If it was Destroy()'d elsewhere, this will be no-op next time due to null guard in Spawn
        pool.Enqueue(go);
    }
}