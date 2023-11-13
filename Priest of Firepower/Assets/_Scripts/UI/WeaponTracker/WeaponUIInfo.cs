using _Scripts.ScriptableObjects;
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
                Debug.Log("ammo in magazine: " + weaponData.ammoInMagazine);
                Debug.Log("magazine size: " + weaponData.magazineSize);
                float fill = weaponData.ammoInMagazine / (float)weaponData.magazineSize;
                magazineSprite.fillAmount = fill;

            }
            


            //show total ammo remaining
            int currentAmmo = weaponData.totalAmmo;
            totalAmmo.text = currentAmmo.ToString() + " / " + (weaponData.maxAmmoCapacity).ToString();

        }

    }
}
