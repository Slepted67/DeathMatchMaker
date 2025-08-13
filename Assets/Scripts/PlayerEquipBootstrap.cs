using UnityEngine;

public class PlayerEquipBootstrap : MonoBehaviour
{
    public WeaponMount mount;                 // drag your WeaponMount here
    public WeaponMeleeData equipOnStart;      // drag one of your weapon assets here

    void Start() {
        Debug.Log($"[Bootstrap] Start. mount? {mount}  equip? {equipOnStart}");
        if (mount && equipOnStart) mount.EquipMelee(equipOnStart);
    }
}
