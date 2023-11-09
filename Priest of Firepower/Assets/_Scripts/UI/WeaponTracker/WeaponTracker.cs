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
        }
        if (index == 1)
        {
            secodnaryWeapon.SetWeapon(data);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
