using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTracker : MonoBehaviour
{
    [SerializeField] WeaponUIInfo primaryWeapon;
    [SerializeField] WeaponUIInfo secodnaryWeapon;


    private void OnEnable()
    {
        WeaponSwitcher.OnWeaponChange += ChangeWeapon;
        PlayerShooter.OnShoot += UpdateWeaponUI;
    }

    void UpdateWeaponUI()
    {
        primaryWeapon.UpdateUI();
        secodnaryWeapon.UpdateUI();
    }

    void ChangeWeapon(GameObject weapon, int index)
    {
        WeaponData data = weapon.GetComponent<Weapon>().localData;

        if (data == null)
        {
            Debug.Log("Error Changing Weapon UI");
            Debug.Log("Weapon: " + weapon + " Index: " + index);
            return;
        }

        if (index == 0)
        {
            primaryWeapon.SetWeapon(data);
            primaryWeapon.UpdateUI();
        }
        if (index == 1)
        {
            secodnaryWeapon.SetWeapon(data);
            secodnaryWeapon.UpdateUI();
        }
    }
}
