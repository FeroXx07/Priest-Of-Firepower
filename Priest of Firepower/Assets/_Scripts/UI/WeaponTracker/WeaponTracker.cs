using _Scripts.Player;
using _Scripts.ScriptableObjects;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.UI.WeaponTracker
{
    public class WeaponTracker : MonoBehaviour
    {
        [SerializeField] WeaponUIInfo primaryWeapon;
        [SerializeField] WeaponUIInfo secodnaryWeapon;


    private void OnEnable()
    {
        WeaponSwitcher.OnWeaponChange += ChangeWeapon;
        PlayerShooter.OnShoot += UpdateWeaponUI;
        PlayerShooter.OnFinishedReload += UpdateWeaponUI;
    }

        void UpdateWeaponUI()
        {
            primaryWeapon.UpdateUI();
            secodnaryWeapon.UpdateUI();
        }

        void ChangeWeapon(GameObject weapon, int index)
        {
            WeaponData data = weapon.GetComponent<Weapon.Weapon>().localData;

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
}
