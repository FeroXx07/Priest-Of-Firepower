using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public KeyCode[] keys;
    public Transform[] holders;
    public GameObject[] weapons;
    int selectedWeapon = 0;

    public float switchTime;
    float lastSwtichTime;

    public static Action<Transform> OnWeaponSwitch;
    private void Start()
    {
        SetWeapons();
        SelectWeapon(selectedWeapon);
    }

    private void SelectWeapon(int selectedWeapon)
    {
        for(int i = 0;i<holders.Length;i++)
        {
            holders[i].gameObject.SetActive(i == selectedWeapon);
        }
        lastSwtichTime = 0;
        OnWeaponSwitch?.Invoke(holders[selectedWeapon]);
        OnWeaponSelected();
    }

    private void SetWeapons()
    {
       
    }

    private void Update()
    {

        int previousWeapon = selectedWeapon;

        for(int i = 0; i<keys.Length; i++)
        {
            if (Input.GetKey(keys[i]) && lastSwtichTime >= switchTime)
            {
                selectedWeapon = i;
            }
        }

        if (selectedWeapon != previousWeapon)
            SelectWeapon(selectedWeapon);

        lastSwtichTime += Time.deltaTime;
    }

    public void ChangeWeapon(GameObject newWeaponPrefab)
    {
       Transform holder = null;

        for(int i = 0; i < holders.Length; i++)
        {
            if(i == selectedWeapon) { 
                holder = holders[i].transform;
            }
        }

        if(holder == null)  return;

        //remove previous weapon
        foreach (Transform w in holder)
        {
            Destroy(w.gameObject);
        }
        
        Instantiate(newWeaponPrefab,holder.transform);
    }
    void OnWeaponSelected()
    {

    }
}
