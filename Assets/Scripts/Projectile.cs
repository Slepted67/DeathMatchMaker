using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;
    public Rigidbody2D rb;
    [HideInInspector] public ProjectilePool pool;   // <-- needed

    void Awake() { if (!rb) rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Despawn), lifeTime);
    }

    public void Fire(Vector2 dir)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        transform.up = dir;
        rb.linearVelocity = dir.normalized * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Despawn();
    }

    void Despawn()
    {
        if (pool) pool.Despawn(gameObject);         // <-- returns to its pool
        else Destroy(gameObject);                   // fallback if no pool
    }
}
