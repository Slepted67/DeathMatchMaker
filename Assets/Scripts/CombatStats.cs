using UnityEngine;

public class CombatStats : MonoBehaviour
{
    [Header("Debug Label")]
    public string label = "Entity";   // e.g., "Player" or "Enemy"

    [Header("Counters (read-only)")]
    public int attacksAttempted;
    public int hitsLanded;

    public void RegisterAttackAttempt()
    {
        attacksAttempted++;
        // Uncomment for spammy per-attempt logs:
        // Debug.Log($"{label} ATTEMPT #{attacksAttempted}");
    }

    public void RegisterHit()
    {
        hitsLanded++;
        Debug.Log($"{label} HIT {hitsLanded}/{attacksAttempted}  ({HitRate()*100f:0.0}% landed)");
    }

    public float HitRate()
    {
        return attacksAttempted <= 0 ? 0f : (float)hitsLanded / attacksAttempted;
    }
}