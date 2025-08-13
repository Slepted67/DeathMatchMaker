using UnityEngine;

public class EnemyEquipBootstrapRanged : MonoBehaviour
{
    public RangedMount mount;
    public WeaponRangedData equipOnStart; // e.g. RW_Bow_Arrow, RW_Staff_Orb, RW_Javelin

    void Start()
    {
        if (mount && equipOnStart) mount.Equip(equipOnStart);
    }
}