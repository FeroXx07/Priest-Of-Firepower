using _Scripts.Player;
using _Scripts.ScriptableObjects;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.WeaponTracker
{
    public class WeaponUIInfo : MonoBehaviour
    {
        [SerializeField] WeaponData weaponData;

        [SerializeField] Image weaponSprite;
        [SerializeField] float spriteSize;
        [SerializeField] Image magazineSprite;
        [SerializeField] TextMeshProUGUI totalAmmo;

        float _prevAmmo;
        float _newAmmo;
        float _currReloadTime;
        float _reloadTime;

        private void OnEnable()
        {
            PlayerShooter.OnStartingReload += Reload;
        }
        private void Awake()
        {
            weaponSprite.preserveAspect = true;
        
        }

        public void SetWeapon(WeaponData data)
        {
            weaponData = data;
            weaponSprite.sprite = data.sprite;

            float a = weaponSprite.sprite.rect.height;
            float b = weaponSprite.sprite.rect.width;
        
            float spriteRatio = b / a * spriteSize;

            weaponSprite.gameObject.transform.localScale = new Vector3(spriteRatio, spriteRatio, spriteRatio);
            UpdateUI();
        }


        public void UpdateUI()
        {

            if (weaponData == null) return;


            //draw remaining bullets in current magazine
            if (weaponData.maxAmmoCapacity != 0)
            {

                UpdateMagazineProgress((float)weaponData.ammoInMagazine, (float)weaponData.magazineSize);
            }
            


            //show total ammo remaining
            int currentAmmo = weaponData.totalAmmo;
            totalAmmo.text = currentAmmo.ToString() + " / " + (weaponData.maxAmmoCapacity).ToString();

        }


        void UpdateMagazineProgress(float currentValue, float maxValue)
        {
            float fill = currentValue / maxValue;
            magazineSprite.fillAmount = fill;
        }

        void Reload()
        {
            if (weaponData == null) return;

            if (weaponData.totalAmmo > weaponData.magazineSize)
            {
                _prevAmmo = weaponData.ammoInMagazine;
                _newAmmo = weaponData.magazineSize;
            }
            else
            {
                _prevAmmo = weaponData.ammoInMagazine;
                _newAmmo = weaponData.totalAmmo;
            }
            _currReloadTime = 0;
            _reloadTime = weaponData.reloadSpeed;

            StartCoroutine(ReloadRoutine());
        }


        IEnumerator ReloadRoutine()
        {
            print("Reloading!!!!!!");
            while (_currReloadTime < _reloadTime)
            {
                _currReloadTime += Time.deltaTime;
                float value = Mathf.Lerp(_prevAmmo, _newAmmo, _currReloadTime / _reloadTime);
                UpdateMagazineProgress(value, (float)weaponData.magazineSize);
                yield return null;
            }

        }
    }
}
