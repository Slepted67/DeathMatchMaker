using UnityEngine;
public class PlayerEquipBootstrapRanged : MonoBehaviour {
    public RangedMount mount;
    public WeaponRangedData equipOnStart;
    void Start() { if (mount && equipOnStart) mount.Equip(equipOnStart); }
}