using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] spawnPoints;                // drop 3–6 empties around the map
    public GameObject meleePrefab;                 // EnemyBase_Melee variant
    public GameObject rangedPrefab;                // EnemyBase_Ranged variant
    public Transform enemyContainer;               // empty parent for cleanliness
    public int WaveNumber => waveIndex;

    [Header("Randomization Pools")]
    public WeaponMeleeData[] meleePool;           // drag your melee SOs here
    public WeaponRangedData[] rangedPool;         // drag your ranged SOs here
    public AIPersonality[] personalities;         // the 4 core personalities
    public AITraitPreset[] mixins;                // the 8 mix-in traits
    public Vector2Int traitCountRange = new Vector2Int(0, 2); // pick 0..2 mix-ins

    [Header("Wave Tuning")]
    public int startCount = 1;
    public int maxCount = 8;
    public float rangedChance = 0.35f;            // probability per spawn
    public float spawnDelay = 0.25f;              // spacing between spawns

    [Header("Difficulty Ramp per wave")]
    public float meleeMoveSpeedAdd = 0.2f;
    public float meleeCooldownMult = 0.97f;
    public float rangedCooldownMult = 0.98f;

    [System.Serializable]
    public struct EnemyColorVariant
    {
        public Sprite body;   // body sprite for this color
        public Sprite handL;  // left-hand sprite matching this color
        public Sprite handR;  // right-hand sprite matching this color
    }

    [Header("Enemy Appearance")]
    public EnemyColorVariant[] enemyLooks;   // size = 4 (your 4 colors)
    public bool handsMatchBody = false;      // if true, both hands use same color as body

    // runtime
    private int waveIndex = 0;
    private readonly List<GameObject> alive = new();

    private void Start()
    {
        if (!enemyContainer)
        {
            var go = new GameObject("Enemies");
            enemyContainer = go.transform;
        }
        StartCoroutine(WaveLoop());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();
            foreach (var e in alive) if (e) Destroy(e);
            alive.Clear();
            waveIndex = 0;
            StartCoroutine(WaveLoop());
        }
    }

    public int AliveCount
    {
        get
        {
            int count = 0;
            foreach (var e in alive)
                if (e && e.activeInHierarchy) count++;
            return count;
        }
    }

    private static void ForceZZeroRecursive(Transform t)
    {
        // zero local Z on this transform and all children
        t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 0f);
        for (int i = 0; i < t.childCount; i++)
            ForceZZeroRecursive(t.GetChild(i));
    }

    private void SanitizeSpawnTransform(Transform root)
    {
        // also clamp world Z on the root (in case parent had Z)
        root.position = new Vector3(root.position.x, root.position.y, 0f);
        ForceZZeroRecursive(root);
    }

    private void OnValidate()
    {
        // keep container on Z=0 in edit mode
        if (enemyContainer)
        {
            var p = enemyContainer.position;
            if (Mathf.Abs(p.z) > 0f) enemyContainer.position = new Vector3(p.x, p.y, 0f);
        }

        // keep all spawn points on Z=0
        if (spawnPoints != null)
        {
            foreach (var sp in spawnPoints)
            {
                if (!sp) continue;
                var pos = sp.position;
                if (Mathf.Abs(pos.z) > 0f) sp.position = new Vector3(pos.x, pos.y, 0f);
            }
        }
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            waveIndex++;
            int count = Mathf.Min(startCount + (waveIndex - 1), maxCount);

            yield return StartCoroutine(SpawnWave(count));

            // wait for clear
            yield return new WaitUntil(() =>
                alive.Count == 0 || alive.TrueForAll(e => e == null || !e.activeInHierarchy));

            alive.RemoveAll(e => e == null || !e.activeInHierarchy);

            // tiny breather
            yield return new WaitForSeconds(0.75f);
        }
    }

    private IEnumerator SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool spawnRanged = Random.value < rangedChance;
            var prefab = spawnRanged ? rangedPrefab : meleePrefab;
            var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // Instantiate ONCE
            var go = Instantiate(prefab, sp.position, Quaternion.identity, enemyContainer);
            alive.Add(go);

            // Randomize sprites (body + hands)
            ApplyRandomLook(go);

            // Equip random weapon (ranged or melee)
            if (spawnRanged)
            {
                if (go.TryGetComponent<RangedMount>(out var rangedMount))
                {
                    var rw = RandomRangedSO();
                    if (rw) rangedMount.Equip(rw);
                    else Debug.LogWarning("RoundManager: rangedPool empty or null SO.");
                }
            }
            else
            {
                if (go.TryGetComponent<WeaponMount>(out var meleeMount))
                {
                    var mw = RandomMeleeSO();
                    if (mw) meleeMount.EquipMelee(mw);
                    else Debug.LogWarning("RoundManager: meleePool empty or null SO.");
                }
            }

            // Light difficulty ramp
            if (!spawnRanged && go.TryGetComponent<EnemyControllerBasic>(out var meleeCtrl))
            {
                meleeCtrl.moveSpeed += meleeMoveSpeedAdd * (waveIndex - 1);
                meleeCtrl.attackCooldown *= Mathf.Pow(meleeCooldownMult, (waveIndex - 1));
            }
            else if (spawnRanged && go.TryGetComponent<EnemyRangedShooter>(out var shooter))
            {
                shooter.shootCooldown *= Mathf.Pow(rangedCooldownMult, (waveIndex - 1));
            }

            // Traits: ensure there’s a build + brain component
            if (!go.TryGetComponent<AITraitBuild>(out var build))
                build = go.AddComponent<AITraitBuild>();
            if (!go.TryGetComponent<EnemyAIBrain>(out var brain))
                brain = go.AddComponent<EnemyAIBrain>();

            build.personality = RandomPersonalitySO();
            build.traits      = RandomTraitArray(traitCountRange.x, traitCountRange.y);

            // optional: force an immediate apply (Start will run too)
            brain.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ---- Appearance ----
    private void ApplyRandomLook(GameObject enemyGO)
    {
        if (enemyLooks == null || enemyLooks.Length == 0) return;

        // Requires a separate EnemyVisualRefs component on the prefab
        var refs = enemyGO.GetComponentInChildren<EnemyVisualRefs>();
        if (!refs || !refs.body || !refs.handL || !refs.handR)
        {
            Debug.LogWarning($"ApplyRandomLook: Missing EnemyVisualRefs or SpriteRenderers on {enemyGO.name}");
            return;
        }

        // Pick a random body color
        int bodyIdx = Random.Range(0, enemyLooks.Length);
        refs.body.sprite = enemyLooks[bodyIdx].body;

        if (handsMatchBody)
        {
            refs.handL.sprite = enemyLooks[bodyIdx].handL;
            refs.handR.sprite = enemyLooks[bodyIdx].handR;
        }
        else
        {
            int lIdx = Random.Range(0, enemyLooks.Length);
            int rIdx = Random.Range(0, enemyLooks.Length);
            refs.handL.sprite = enemyLooks[lIdx].handL;
            refs.handR.sprite = enemyLooks[rIdx].handR;
        }
    }

    // ---- Random helpers ----
    private T RandomFrom<T>(IList<T> arr) where T : Object
    {
        if (arr == null || arr.Count == 0) return null;
        return arr[Random.Range(0, arr.Count)];
    }

    private WeaponMeleeData RandomMeleeSO()     => RandomFrom(meleePool);
    private WeaponRangedData RandomRangedSO()   => RandomFrom(rangedPool);
    private AIPersonality RandomPersonalitySO() => RandomFrom(personalities);

    private AITraitPreset[] RandomTraitArray(int minCount, int maxCount)
    {
        if (mixins == null || mixins.Length == 0 || maxCount <= 0)
            return System.Array.Empty<AITraitPreset>();

        int count = Random.Range(minCount, maxCount + 1);
        count = Mathf.Min(count, mixins.Length);

        // pick without replacement
        var bag = new List<AITraitPreset>(mixins);
        var picked = new List<AITraitPreset>(count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, bag.Count);
            picked.Add(bag[idx]);
            bag.RemoveAt(idx);
        }
        return picked.ToArray();
    }
}