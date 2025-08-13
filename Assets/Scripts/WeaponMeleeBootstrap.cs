using UnityEngine;

public class EnemyMeleeBootstrap : MonoBehaviour
{
    public WeaponMeleeData defaultWeapon;

    void Awake()
    {
        if (TryGetComponent<WeaponMount>(out var mount) && defaultWeapon != null)
        {
            mount.EquipMelee(defaultWeapon);
        }
    }
}