using System;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public KeyCode[] keys;
    public WeaponSlot[] slots;
    int selectedWeapon = 0;

    public float switchTime;
    float lastSwtichTime;

    public static Action<Transform> OnWeaponSwitch;
    public static Action<GameObject, int> OnWeaponChange;

    [SerializeField] GameObject initialWeaponPrefab;
    [SerializeField] GameObject initialSecondaryWeaponPrefab;

    [Serializable]
    public struct WeaponSlot
    {
        public Transform holder;
        public GameObject weapon;
        public bool Empty;
        public int index;
    }

    private void Start()
    {
        SetWeapons();
        SelectWeapon(selectedWeapon);
        ChangeWeapon(initialWeaponPrefab);
        ChangeWeapon(initialSecondaryWeaponPrefab);
    }

    private void SelectWeapon(int selectedWeapon)
    {
        for(int i = 0;i< slots.Length;i++)
        {
            slots[i].holder.gameObject.SetActive(i == selectedWeapon);
        }
        lastSwtichTime = 0;
        OnWeaponSwitch?.Invoke(slots[selectedWeapon].holder);
        OnWeaponSelected();
    }

    private void SetWeapons()
    {
        //clean up the weapons slots
        for(int i =0;i< slots.Length;i++)
        {
            slots[i].Empty = true;
            slots[i].holder.gameObject.SetActive(false);
            slots[i].weapon = null;
            slots[i].index = i;
        }
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
        if (newWeaponPrefab == null) return;

        WeaponSlot emptySlot = new WeaponSlot{Empty = true, holder = null, weapon = null,index = -1 };

        //check if one of the slots is empty, if so add the new weapon there
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].Empty)
            {
                emptySlot = slots[i];
                emptySlot.Empty = false;
                emptySlot.index = i;
                break;
            }
        }

        //if the emptySlot is still empty means that other slots are full
        //then change the weapon to the currently selected weapon
        if(emptySlot.Empty)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i == selectedWeapon)
                {
                    emptySlot = slots[i];
                    break;
                }
            }
        }

        //remove previous weapon
        foreach (Transform w in emptySlot.holder)
        {
            Destroy(w.gameObject);
        }

        emptySlot.weapon = newWeaponPrefab;
        slots[emptySlot.index] = emptySlot;

        GameObject weapon = Instantiate(newWeaponPrefab,emptySlot.holder.transform);
        weapon.GetComponent<Weapon>().SetData();
        weapon.GetComponent<Weapon>().SetOwner(gameObject);

        OnWeaponChange?.Invoke(weapon, emptySlot.index);

    }
    void OnWeaponSelected()
    {
        //TODO add sound 
    }
    public Weapon GetSelectedWeapon()
    {
        Weapon wp = slots[selectedWeapon].weapon.GetComponent<Weapon>();
        if (wp != null)
            return wp;

        return null;
    }
}
