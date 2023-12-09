using System;
using _Scripts.Networking;
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
        private Player.Player lastPlayerShooter;
        private void OnEnable()
        {
            WeaponSwitcher weaponSwitcher = NetworkManager.Instance.player.GetComponent<WeaponSwitcher>();
            weaponSwitcher.OnWeaponChange += ChangeWeapon;

            if (lastPlayerShooter)
            {
                lastPlayerShooter.OnShoot -= UpdateWeaponUI;
                lastPlayerShooter.OnFinishedReload -= UpdateWeaponUI;
            }
        }

        private void OnDisable()
        {
            WeaponSwitcher weaponSwitcher = NetworkManager.Instance.player.GetComponent<WeaponSwitcher>();
        }

        void UpdateWeaponUI()
        {
            primaryWeapon.UpdateUI();
            secodnaryWeapon.UpdateUI();
        }

        void ChangeWeapon(Player.Player shooter, GameObject weapon, int index)
        {
            lastPlayerShooter = shooter;
            shooter.OnShoot += UpdateWeaponUI;
            shooter.OnFinishedReload += UpdateWeaponUI;
            
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