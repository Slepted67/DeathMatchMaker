using UnityEngine;

[System.Serializable]
public struct AIIntentProfile
{
    public float desiredRange;
    public float rangeSlack;
    public float aggression;
    public float retreatAtHp;
    public float attackCadenceMult;
    public float strafeBias;
    public float flankAnglePref;
    public float repositionCooldown;
    public float losStrictness;
    public float burstWindow;
    public float packStickiness;
    public float kiteBias;

    public bool preferCover;
    public bool earlyCommit;
    public bool latePunish;

    public static AIIntentProfile Baseline(float weaponSuggestedRange)
    {
        return new AIIntentProfile
        {
            desiredRange = weaponSuggestedRange, // derived from weapon
            rangeSlack = 0.9f,
            aggression = 0.5f,
            retreatAtHp = 0.25f,
            attackCadenceMult = 1f,
            strafeBias = 0.35f,
            flankAnglePref = 20f,
            repositionCooldown = 1.0f,
            losStrictness = 0.3f,
            burstWindow = 0.0f,
            packStickiness = 0.2f,
            kiteBias = 0.0f,
            preferCover = false,
            earlyCommit = false,
            latePunish = false
        };
    }

    public void Apply(AITraitPreset t)
    {
        desiredRange = Mathf.Max(0.5f, desiredRange);
        rangeSlack = Mathf.Clamp(rangeSlack, 0.7f, 0.98f);
        aggression = Mathf.Clamp01(aggression + t.aggressionDelta);
        retreatAtHp = Mathf.Clamp01(retreatAtHp + t.retreatAtHpDelta);
        attackCadenceMult = Mathf.Clamp(attackCadenceMult, 0.5f, 1.8f);
        strafeBias = Mathf.Clamp01(strafeBias + t.strafeBiasAdd);
        flankAnglePref = Mathf.Clamp(flankAnglePref + t.flankAnglePrefAdd, 0f, 90f);
        repositionCooldown = Mathf.Max(0.1f, repositionCooldown + t.repositionCooldownDelta);
        losStrictness = Mathf.Clamp01(losStrictness + t.losStrictnessDelta);
        burstWindow = Mathf.Max(0f, burstWindow + t.burstWindowAdd);
        packStickiness = Mathf.Clamp01(packStickiness + t.packStickinessAdd);
        kiteBias = Mathf.Clamp01(kiteBias + t.kiteBiasAdd);

        preferCover |= t.preferCover;
        earlyCommit |= t.earlyCommit;
        latePunish |= t.latePunish;
    }
}