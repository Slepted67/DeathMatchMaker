using UnityEngine;

[CreateAssetMenu(menuName="AI/Trait Preset")]
public class AITraitPreset : ScriptableObject
{
    [Header("Core Intent (deltas, not absolutes)")]
    [Range(-4f, +4f)] public float desiredRangeDelta = 0f;
    [Range(-0.5f, 0.5f)] public float rangeSlackDelta = 0f;
    [Range(-1f, +1f)] public float aggressionDelta = 0f;
    [Range(-0.5f, +0.5f)] public float retreatAtHpDelta = 0f;
    [Range(0.5f, 1.5f)] public float attackCadenceMult = 1f;
    [Range(0f, 1f)] public float strafeBiasAdd = 0f;
    [Range(0f, 60f)] public float flankAnglePrefAdd = 0f;
    [Range(-1f, +1f)] public float repositionCooldownDelta = 0f;
    [Range(-1f, +1f)] public float losStrictnessDelta = 0f;
    [Range(0f, 1.5f)] public float burstWindowAdd = 0f;
    [Range(0f, 1f)] public float packStickinessAdd = 0f;
    [Range(0f, 1f)] public float kiteBiasAdd = 0f;

    [Header("Situational toggles")]
    public bool preferCover;        // controller/survivor
    public bool earlyCommit;        // charger/berserker
    public bool latePunish;         // opportunist/skirmisher
}