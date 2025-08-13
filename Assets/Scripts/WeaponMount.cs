using System.Collections;
using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    [Header("Refs")]
    public Transform visualForward;          // your Visual transform (+Y forward)
    public Transform rightHand;              // optional; preferred parent for the visual
    public Transform leftHand;               // optional; future 2H support
    public MeleeOverlapScanner meleeScanner; // on the same GO as PlayerCombat2D
    public BoxCollider2D meleeShape;         // collider under AttackRoot (shape only)
    public Transform attackRoot;             // child under Visual (placement only)

    [Header("Equipped")]
    public WeaponMeleeData currentMelee;

    // runtime state
    private GameObject visualInstance;
    private Coroutine swingCo;

    public void EquipMelee(WeaponMeleeData data)
    {
        currentMelee = data;
        if (!currentMelee) { CleanupVisual(); return; }

        // 1) Apply collider shape
        if (meleeShape)
        {
            meleeShape.size = currentMelee.colliderSize;
            meleeShape.offset = currentMelee.colliderOffset;
        }

        // 2) Apply scanner params
        // Apply scanner parameters
        if (meleeScanner)
        {
            meleeScanner.damage = currentMelee.damage;
            meleeScanner.swingDuration = currentMelee.swingDuration;

            // NEW: sweep-driven
            meleeScanner.sweepArcDeg   = currentMelee.sweepArcDeg;
            meleeScanner.sweepSamples  = currentMelee.sweepSamples;
            meleeScanner.originOffset  = currentMelee.originOffset;

            if (visualForward) meleeScanner.forwardRef = visualForward;
        }


        // 3) Keep AttackRoot in front of body
        if (attackRoot)
        {
            attackRoot.localPosition = new Vector3(0f, currentMelee.colliderOffset.y, 0f);
            attackRoot.localRotation = Quaternion.identity;
        }

        // 4) Spawn/refresh the visual in-hand
        SpawnVisual();

        Debug.Log($"Equipped melee: {currentMelee.displayName}");
    }

    public void PlaySwingVisual()
    {
        if (!visualInstance || !currentMelee) return;
        if (swingCo != null) StopCoroutine(swingCo);
        swingCo = StartCoroutine(SwingAnim(currentMelee.swingDuration));
    }

    private void SpawnVisual()
    {
        CleanupVisual();

        if (!currentMelee.visualPrefab)
        {
            Debug.LogWarning("WeaponMount: visualPrefab is null on the equipped weapon.");
            return;
        }

        // Prefer the hand as parent; fallback to Visual; final fallback to self
        Transform parent = rightHand ? rightHand : (visualForward ? visualForward : transform);
        visualInstance = Instantiate(currentMelee.visualPrefab, parent);

        // Reset local TRS, then apply data’s alignment (your pivots are already at the grip)
        var t = visualInstance.transform;
        t.localPosition = (Vector3)currentMelee.gripLocalPos;     // usually (0,0)
        t.localRotation = Quaternion.Euler(0, 0, currentMelee.gripLocalRotDeg);
        t.localScale    = currentMelee.visualLocalScale;

        // Force correct sorting so it renders above the body/hands
        var refSR = parent.GetComponentInChildren<SpriteRenderer>();
        var sr    = visualInstance.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            if (refSR)
            {
                sr.sortingLayerID = refSR.sortingLayerID;
                sr.sortingOrder   = refSR.sortingOrder + 5; // draw above hands/body
            }
            else
            {
                sr.sortingLayerName = "Player"; // fallback layer name if you use one
                sr.sortingOrder     = 10;
            }
            sr.enabled = true;
        }

        // Ensure Z=0 with the character
        t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 0f);
    }

    private IEnumerator SwingAnim(float duration)
    {
        // small wind-up → follow-through rotation around the grip
        Transform t = visualInstance.transform;
        float arc = currentMelee.twoHanded ? 35f : 25f;
        float half = duration * 0.5f;
        float t0 = 0f;

        // wind-up (counter-rotate)
        while (t0 < half)
        {
            t0 += Time.deltaTime;
            float a = Mathf.Lerp(0f, -arc * 0.5f, t0 / half);
            t.localRotation = Quaternion.Euler(0, 0, currentMelee.gripLocalRotDeg + a);
            yield return null;
        }
        // release
        t0 = 0f;
        while (t0 < half)
        {
            t0 += Time.deltaTime;
            float a = Mathf.Lerp(-arc * 0.5f, arc, t0 / half);
            t.localRotation = Quaternion.Euler(0, 0, currentMelee.gripLocalRotDeg + a);
            yield return null;
        }
        // settle
        t.localRotation = Quaternion.Euler(0, 0, currentMelee.gripLocalRotDeg);
        swingCo = null;
    }

    private void CleanupVisual()
    {
        if (visualInstance) Destroy(visualInstance);
        visualInstance = null;
    }
}