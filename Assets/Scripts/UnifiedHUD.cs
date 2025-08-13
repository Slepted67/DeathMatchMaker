using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnifiedHUD : MonoBehaviour
{
    [Header("Refs")]
    public RoundManager roundManager;   // optional; works even if you don't expose wave/alive
    public CombatStats playerStats;     // REQUIRED for player hit%
    public CombatStats enemyStats;      // OPTIONAL: show an enemy's stats (e.g., last spawned)

    [Header("UI")]
    public TMP_Text waveText;               // "Wave: X"
    public TMP_Text enemiesText;            // "Enemies: N | Player Hit%: Y"
    public TMP_Text statsText;              // "Player: a/b (X%)\nEnemy: c/d (Y%)" (second line optional)

    void Update()
    {
        // ---- Wave # (if available) ----
        int wave = TryGetWaveNumber(roundManager);
        if (waveText)
        {
            waveText.text = (wave > 0) ? $"Wave: {wave}" : "Wave: —";
        }

        // ---- Enemies alive + Player Hit% ----
        int alive = TryGetAliveCount(roundManager);
        if (enemiesText)
        {
            float playerHit = Rate(playerStats);
            enemiesText.text = $"Enemies: {alive}  |  Player Hit%: {playerHit:0}%";
        }

        // ---- Detailed stats block (optional) ----
        if (statsText)
        {
            string playerLine = playerStats
                ? $"{playerStats.label}: {playerStats.hitsLanded}/{playerStats.attacksAttempted} ({Rate(playerStats):0.0}%)"
                : "Player: —";

            if (enemyStats)
            {
                string enemyLine  = $"{enemyStats.label}: {enemyStats.hitsLanded}/{enemyStats.attacksAttempted} ({Rate(enemyStats):0.0}%)";
                statsText.text = playerLine + "\n" + enemyLine;
            }
            else
            {
                statsText.text = playerLine;
            }
        }
    }

    // ---- helpers ----

    // Prefer a public property on RoundManager like:
    //   public int WaveNumber => waveIndex;
    // If it's not there yet, we return 0 and show "—".
    private int TryGetWaveNumber(RoundManager rm)
    {
        if (!rm) return 0;

        // If you've added the property, this will compile & work:
        // return rm.WaveNumber;

        // Fallback: unknown
        return 0;
    }

    // Prefer a public property on RoundManager like:
    //   public int AliveCount => alive.Count(e => e && e.activeInHierarchy);
    // If it's not there yet, we fallback to a scene search.
    private int TryGetAliveCount(RoundManager rm)
    {
        // Fallback: count enemies in the scene (simple but fine for testing)
        return FindObjectsByType<EnemyControllerBasic>(FindObjectsSortMode.None).Length + FindObjectsByType<EnemyRangedShooter>(FindObjectsSortMode.None).Length;
    }

    private float Rate(CombatStats s)
    {
        if (!s || s.attacksAttempted <= 0) return 0f;
        return 100f * (float)s.hitsLanded / Mathf.Max(1, s.attacksAttempted);
    }
}
