using UnityEngine;

public class AITraitBuild : MonoBehaviour
{
    [Header("Pick ONE personality")]
    public AIPersonality personality;

    [Header("Pick 0-3 mix-ins")]
    public AITraitPreset[] traits;

    [Header("Debug")]
    public bool logProfile;
}