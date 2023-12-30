using _Scripts.Networking;
using _Scripts.ScriptableObjects;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.UI.WeaponTracker
{
    public class WeaponTracker : MonoBehaviour
    {
        [SerializeField] private WeaponUIInfo primaryWeapon;
        [SerializeField] private WeaponUIInfo secondaryWeapon;
        [SerializeField] private Player.Player playerOwner;
        [SerializeField] private WeaponSwitcher playerWeaponSwitcher;
        private void OnEnable()
        {
            NetworkManager.Instance.OnHostPlayerCreated += Init;
        }

        private void OnDisable()
        {
            NetworkManager.Instance.OnHostPlayerCreated -= Init;
            if (playerWeaponSwitcher) playerWeaponSwitcher.OnWeaponChange -= ChangeWeapon;
        }
        void FindAndSetPlayer()
        {
            playerOwner = NetworkManager.Instance.player.GetComponent<Player.Player>();
        }
        private void Init(GameObject obj)
        {
            FindAndSetPlayer();
            
            if (playerOwner == null)
            {
                Debug.LogError("No player instance!");
                return;
            }
            
            playerWeaponSwitcher = playerOwner.GetComponent<WeaponSwitcher>();
            playerWeaponSwitcher.OnWeaponChange += ChangeWeapon;
            
            if (playerOwner)
            {
                Debug.Log("Weapon Tracker: Unsubscribing UpdateWeaponUI");
                playerOwner.OnShoot -= UpdateWeaponUI;
                playerOwner.OnFinishedReload -= UpdateWeaponUI;
            }
        }
        
        void UpdateWeaponUI(WeaponData data)
        {
            //Debug.Log("Weapon Tracker: UpdateWeaponUI()");
            if (primaryWeapon) primaryWeapon.UpdateUI();
            if (secondaryWeapon) secondaryWeapon.UpdateUI();
        }

        void ChangeWeapon(Player.Player shooter, GameObject weapon, int index)
        {
            Debug.Log("Weapon Tracker: ChangeWeapon");

            if (playerOwner == shooter && shooter.isOwner())
            {
                Debug.Log("Weapon Tracker: Subscribing UpdateWeaponUI");
                shooter.OnShoot += UpdateWeaponUI;
                shooter.OnFinishedReload += UpdateWeaponUI;
            }
            
            WeaponData data = weapon.GetComponent<Weapon.Weapon>().localData;
            
            if (data == null)
            {
                Debug.Log($"Weapon Tracker: Error changing weapon. Weapon: {weapon} Index: {index}");
                return;
            }

            switch (index)
            {
                case 0:
                {
                    Debug.Log("Weapon Tracker: Primary Weapon Set");
                    primaryWeapon.SetWeapon(data);
                    primaryWeapon.UpdateUI();
                }
                    break;
                case 1:
                {
                    Debug.Log("Weapon Tracker: Secondary Weapon Set");
                    secondaryWeapon.SetWeapon(data);
                    secondaryWeapon.UpdateUI();
                }
                    break;
            }
        }
    }
}