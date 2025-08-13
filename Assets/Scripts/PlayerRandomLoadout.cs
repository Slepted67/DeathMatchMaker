using UnityEngine;

public class PlayerRandomLoadout : MonoBehaviour
{
    [Header("Pools (drag your SOs here)")]
    public WeaponMeleeData[] meleePool;
    public WeaponRangedData[] rangedPool;

    [Header("Options")]
    [Range(0f,1f)] public float chanceRanged = 0.5f; // 50/50 melee vs ranged
    public bool pickOnStart = true;                  // auto-roll on Start
    public bool allowRerollWithKey = true;          // for testing
    public KeyCode rerollKey = KeyCode.L;

    private WeaponMount meleeMount;
    private RangedMount rangedMount;

    void Awake()
    {
        meleeMount  = GetComponent<WeaponMount>();
        rangedMount = GetComponent<RangedMount>();
    }

    void Start()
    {
        if (pickOnStart) RollAndEquip();
    }

    void Update()
    {
        if (allowRerollWithKey && Input.GetKeyDown(rerollKey))
            RollAndEquip();
    }

    public void RollAndEquip()
    {
        bool equipRanged = Random.value < chanceRanged;

        // Clear visuals by re-equipping null if desired (optional)
        // For now we just equip the new one over the old.

        if (equipRanged && rangedMount && rangedPool != null && rangedPool.Length > 0)
        {
            var so = rangedPool[Random.Range(0, rangedPool.Length)];
            rangedMount.Equip(so);
            Debug.Log($"Player equipped RANGED: {(so ? so.displayName : "null")}");
        }
        else if (meleeMount && meleePool != null && meleePool.Length > 0)
        {
            var so = meleePool[Random.Range(0, meleePool.Length)];
            meleeMount.EquipMelee(so);
            Debug.Log($"Player equipped MELEE: {(so ? so.displayName : "null")}");
        }
    }
}