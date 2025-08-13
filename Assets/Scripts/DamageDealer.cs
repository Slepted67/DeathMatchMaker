using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageDealer : MonoBehaviour
{
    public GameObject owner;           // set by spawner
    public float damage = 10f;
    public LayerMask hitLayers = ~0;   // set by spawner based on team
    public bool ignoreOwnerRoot = true;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ignoreOwnerRoot && owner && other.transform.IsChildOf(owner.transform))
            return;

        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        var h = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (!h) return;

        h.TakeDamage(damage);

        if (owner && owner.TryGetComponent<CombatStats>(out var stats))
            stats.RegisterHit();
    }
}