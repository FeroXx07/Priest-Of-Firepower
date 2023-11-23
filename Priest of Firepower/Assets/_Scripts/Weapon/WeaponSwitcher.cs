using System;
using _Scripts.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Weapon
{
    public class WeaponSwitcher : MonoBehaviour
    {
        public KeyCode[] keys;
        public WeaponSlot[] slots;
        int _selectedWeapon = 0;

        public float switchTime;
        float _lastSwtichTime;

        public Action<Transform> OnWeaponSwitch;
        public Action<PlayerShooter, GameObject, int> OnWeaponChange;

        [SerializeField] GameObject initialWeaponPrefab;
        [SerializeField] GameObject initialSecondaryWeaponPrefab;

        [Serializable]
        public struct WeaponSlot
        {
            public Transform holder;
            public GameObject weapon;
            [FormerlySerializedAs("Empty")] public bool empty;
            public int index;
        }

        private void Start()
        {
            SetWeapons();
            SelectWeapon(_selectedWeapon);
            ChangeWeapon(initialWeaponPrefab);
            ChangeWeapon(initialSecondaryWeaponPrefab);
        }

        private void SelectWeapon(int selectedWeapon)
        {
            for(int i = 0;i< slots.Length;i++)
            {
                slots[i].holder.gameObject.SetActive(i == selectedWeapon);
            }
            _lastSwtichTime = 0;
            OnWeaponSwitch?.Invoke(slots[selectedWeapon].holder);
            OnWeaponSelected();
        }

        private void SetWeapons()
        {
            //clean up the weapons slots
            for(int i = 0;i < slots.Length;i++)
            {
                slots[i].empty = true;
                if (slots[i].holder != null)
                    slots[i].holder.gameObject.SetActive(false);
                slots[i].weapon = null;
                slots[i].index = i;
            }
        }

        private void Update()
        {
            int previousWeapon = _selectedWeapon;

            for(int i = 0; i<keys.Length; i++)
            {
                if (Input.GetKey(keys[i]) && _lastSwtichTime >= switchTime)
                {
                    _selectedWeapon = i;
                }
            }

            if (_selectedWeapon != previousWeapon)
                SelectWeapon(_selectedWeapon);

            _lastSwtichTime += Time.deltaTime;
        }

        public void ChangeWeapon(GameObject newWeaponPrefab)
        {
            if (newWeaponPrefab == null) return;

            WeaponSlot emptySlot = new WeaponSlot{empty = true, holder = null, weapon = null,index = -1 };

            //check if one of the slots is empty, if so add the new weapon there
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].empty)
                {
                    emptySlot = slots[i];
                    emptySlot.empty = false;
                    emptySlot.index = i;
                    break;
                }
            }

            //if the emptySlot is still empty means that other slots are full
            //then change the weapon to the currently selected weapon
            if(emptySlot.empty)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i == _selectedWeapon)
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
            Weapon weaponComponent =  weapon.GetComponent<Weapon>();
            weaponComponent.SetData();
            weaponComponent.SetOwner(gameObject);
            OnWeaponChange?.Invoke(gameObject.GetComponent<PlayerShooter>(), weapon, emptySlot.index);

        }
        void OnWeaponSelected()
        {
            //TODO add sound 
        }
        public Weapon GetSelectedWeapon()
        {
            Weapon wp = slots[_selectedWeapon].weapon.GetComponent<Weapon>();
            if (wp != null)
                return wp;

            return null;
        }
    }
}
