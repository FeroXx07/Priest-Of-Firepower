using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [SerializeField] GameObject weaponHolder1;
    [SerializeField] GameObject weaponHolder2;
    [SerializeField] GameObject statingGunPrefab;
    private void Start()
    {
        SetPrimaryWeapon(statingGunPrefab);
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha1))
            SwitchToPrimaryWeapon();
        if(Input.GetKeyUp(KeyCode.Alpha2))
            SwitchToSecondaryWeapon();
    }

    void SwitchToPrimaryWeapon()
    {
        weaponHolder1.SetActive(true);
        weaponHolder2.SetActive(false);
    }
    void SwitchToSecondaryWeapon()
    {
        weaponHolder1.SetActive(true);
        weaponHolder2.SetActive(false);
    }

    void SetPrimaryWeapon(GameObject weaponPrefab)
    {
        //remove previous weapon
        foreach (GameObject w in weaponHolder1.transform)
        {
            Destroy(w);
        }
        
        Instantiate(weaponPrefab,weaponHolder1.transform);
    }

    void SecondaryWeapon(GameObject weaponPrefab)
    {
        //remove previous weapon
        foreach (GameObject w in weaponHolder1.transform)
        {
            Destroy(w);
        }
        Instantiate(weaponPrefab, weaponHolder2.transform);
    }
}
