using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public float maxHP = 100f;
    public UnityEvent onDeath;

    [Header("Debug")]
    public bool logOnHit = true;
    public string debugLabelOverride = ""; // leave empty to auto-derive from Tag

    private float hp;
    private string debugLabel;

    private void Awake()
    {
        hp = maxHP;
        // auto-label: prefer explicit override, else use Tag, else use name
        if (!string.IsNullOrWhiteSpace(debugLabelOverride))
            debugLabel = debugLabelOverride;
        else if (CompareTag("Player"))
            debugLabel = "Player";
        else if (CompareTag("Enemy"))
            debugLabel = "Enemy";
        else
            debugLabel = gameObject.name;
    }

    public void TakeDamage(float dmg)
    {
        if (hp <= 0) return;

        float before = hp;
        hp -= dmg;

        if (logOnHit)
        {
            Debug.Log($"{debugLabel} HP: {Mathf.Max(hp,0):0}  (-{dmg:0})  [was {before:0}]");
        }

        if (hp <= 0f)
        {
            hp = 0f;
            onDeath?.Invoke();
            gameObject.SetActive(false);
        }
    }

    public float GetHP() => hp;
    public void ResetHP() => hp = maxHP;
}