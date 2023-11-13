using System.Collections;
using _Scripts.Attacks;
using _Scripts.Object_Pool;
using _Scripts.Player;
using _Scripts.ScriptableObjects;
using UnityEngine;
using UnityEngine.VFX;

namespace _Scripts.Weapon
{
    public class Weapon : MonoBehaviour
    {
        #region Fields
        public WeaponData weaponData;
        public WeaponData localData;

        [SerializeField] GameObject bulletRef; //for testing
        [SerializeField] Transform firePoint;

        private float _timeSinceLastShoot;

        private SpriteRenderer _spriteRenderer;
        [SerializeField] VisualEffect muzzleFlash;

        GameObject _owner;

        bool _localDataCopied = false;
        #endregion

        private void Awake()
        {
            if (!_localDataCopied) SetData();
        }

        public void SetData() //forces to copy the data, even if the parents are unactive
        {
            if (!_localDataCopied)
            {

                localData = Instantiate(weaponData); // We don't want to modify the global weapon template, but only ours weapon!
                _localDataCopied = true;
            }
        }

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = localData.sprite;
            _timeSinceLastShoot = 10;

        }
        private void OnEnable()
        {
            PlayerShooter.OnShoot += Shoot;
            PlayerShooter.OnReload += Reload;
            PlayerShooter.OnFlip+= FlipGun;
        }
        private void OnDisable()
        {
            localData.reloading = false;
            PlayerShooter.OnShoot -= Shoot;
            PlayerShooter.OnReload -= Reload;
            PlayerShooter.OnFlip -= FlipGun;
        }
        private void Update()
        {
            _timeSinceLastShoot += Time.deltaTime;
        }

        #region Reload
        void Reload()
        {
            if (localData.reloading || localData.totalAmmo <= 0 || localData.ammoInMagazine >= localData.magazineSize || !gameObject.activeSelf) return;

            PlayerShooter.OnStartingReload?.Invoke();
            StartCoroutine(Realoading());
        }
        IEnumerator Realoading()
        { 
            localData.reloading = true;

            yield return new WaitForSeconds(localData.reloadSpeed);

            if(localData.totalAmmo > 0)
            {

            if (localData.totalAmmo > localData.magazineSize)
                localData.ammoInMagazine = localData.magazineSize;
            else
                localData.ammoInMagazine = localData.totalAmmo;

                localData.reloading = false;
            }

        PlayerShooter.OnFinishedReload?.Invoke();
    }
    #endregion

        #region Shoot
        bool CanShoot()
        {
            //if is reloading or the fire rate is less than the current fire time
            return !localData.reloading && _timeSinceLastShoot > 1 / localData.fireRate / 60;
        }
        void Shoot()
        {
            if (localData.ammoInMagazine > 0)
            {
                if (CanShoot())
                { 
                    GameObject bullet = null;

                    bullet = PoolManager.Instance.Pull(bulletRef);

                    OnTriggerAttack onTriggerAttack = bullet.GetComponent<OnTriggerAttack>();
                    onTriggerAttack.Damage = localData.damage;
                    onTriggerAttack.SetOwner(_owner);

                    transform.localRotation = transform.parent.rotation;

                    float dispersion;
                    dispersion = Random.Range(-localData.dispersion, localData.dispersion);

                    Quaternion newRot = Quaternion.Euler(transform.localEulerAngles.x,
                        transform.localEulerAngles.y,
                        transform.localEulerAngles.z + dispersion);

                    transform.rotation = newRot;
                    bullet.transform.rotation = transform.rotation;
                    bullet.transform.position = firePoint.position;
                    bullet.GetComponent<Rigidbody2D>().velocity = transform.right * localData.bulletSpeed;

                    localData.ammoInMagazine--;
                    localData.totalAmmo--;
                    _timeSinceLastShoot = 0;
                    OnGunShoot();
                }
            }
            else
            {
                Reload();
            }

        }
        void OnGunShoot()
        {
            //VFX, sound

            muzzleFlash.Play();
        }
        #endregion
        void FlipGun(bool flip)
        {
            //_spriteRenderer.flipY = flip;
            if (flip)
            {
                transform.localScale = new Vector3(-1, -1, 1);
            }
            else
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
        }
    

        public void GiveMaxAmmo()
        {
            localData.totalAmmo = localData.maxAmmoCapacity;
            PlayerShooter.OnReload?.Invoke();
        }

        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }
        public GameObject GetOwner()
        {
            return _owner;
        }
    }
}
