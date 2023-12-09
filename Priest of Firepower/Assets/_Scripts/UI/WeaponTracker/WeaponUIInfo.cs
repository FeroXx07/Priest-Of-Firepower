using System;
using _Scripts.Player;
using _Scripts.ScriptableObjects;
using _Scripts.Power_Ups;
using System.Collections;
using _Scripts.Networking;
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
        [SerializeField] Image magazineSpriteBg;
        [SerializeField] TextMeshProUGUI totalAmmo;
        float _prevAmmo;
        float _newAmmo;
        float _currReloadTime;
        float _reloadTime;
        bool _pulsingMagazineBg = false;
        bool _stopMagazineBgPulse = false;
        
        private void Awake()
        {
            weaponSprite.preserveAspect = true;
            
        }      
        private void OnEnable()
        {
            Player.Player shooter = NetworkManager.Instance.player.GetComponent<Player.Player>();
            shooter.OnStartingReload += Reload;
            shooter.OnShoot += TryShoot;
            PowerUpBase.PowerUpPickedGlobal += OnPowerUp;
        }

        private void OnDisable()
        {
            Player.Player shooter = NetworkManager.Instance.player.GetComponent<Player.Player>();
            shooter.OnStartingReload -= Reload;
            shooter.OnShoot -= TryShoot;
            PowerUpBase.PowerUpPickedGlobal -= OnPowerUp;
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
            if (weaponData.totalAmmo <= 0)
            {
                UseFailed();
                return;
            }

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

        void UseFailed()
        {
            if (_pulsingMagazineBg)
            {
                _stopMagazineBgPulse = true;
            }

            StartCoroutine(PulseColor(magazineSpriteBg, Color.red, new Color(0.5f, 0.5f, 0.5f), 0.3f));
            _stopMagazineBgPulse = false;
        }

        void TryShoot()
        {
            if (weaponData == null) return;
            if (weaponData.totalAmmo <= 0)
            {
                UseFailed();
            }
        }

        IEnumerator ReloadRoutine()
        {
            while (_currReloadTime < _reloadTime)
            {
                _currReloadTime += Time.deltaTime;
                float value = Mathf.Lerp(_prevAmmo, _newAmmo, _currReloadTime / _reloadTime);
                UpdateMagazineProgress(value, (float)weaponData.magazineSize);
                yield return null;
            }
        }

        IEnumerator PulseColor(Image image, Color startColor, Color endColor, float time)
        {
            _pulsingMagazineBg = true;
            float timer = 0;
            yield return _pulsingMagazineBg;
            while (timer < time)
            {
                timer += Time.deltaTime;
                image.color = Color.Lerp(startColor, endColor, timer / time);
                if (_stopMagazineBgPulse)
                {
                    image.color = endColor;
                    break;
                }

                yield return null;
            }

            _pulsingMagazineBg = false;
            yield return null;
        }

        void OnPowerUp(PowerUpBase.PowerUpType type)
        {
            if (weaponData == null) return;
            switch (type)
            {
                case PowerUpBase.PowerUpType.MAX_AMMO:
                    break;
                case PowerUpBase.PowerUpType.NUKE:
                    break;
                case PowerUpBase.PowerUpType.DOUBLE_POINTS:
                    break;
                case PowerUpBase.PowerUpType.ONE_SHOT:
                    break;
                default:
                    break;
            }
        }
    }
}