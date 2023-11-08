using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

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

    GameObject Owner;
    #endregion

    private void Awake()
    {
        localData = Instantiate(weaponData); // We don't want to modify the global weapon template, but only ours weapon!
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
        localData.totalAmmo = localData.maxAmmoCapacity;
        localData.ammoInMagazine = localData.magazineSize;
    }
    private void OnDisable()
    {
        localData.Reloading = false;
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
        if (localData.Reloading || localData.totalAmmo <= 0 || localData.ammoInMagazine >= localData.magazineSize || !gameObject.activeSelf) return;

        StartCoroutine(Realoading());
    }
    IEnumerator Realoading()
    { 
        localData.Reloading = true;

        yield return new WaitForSeconds(localData.reloadSpeed);

        if(localData.totalAmmo > 0)
        {
            int bulletsToReload = (localData.magazineSize - localData.ammoInMagazine);

            localData.totalAmmo -= bulletsToReload;
            localData.ammoInMagazine = localData.magazineSize;
            localData.Reloading = false;
        }

    }
    #endregion

    #region Shoot
    bool CanShoot()
    {
        //if is reloading or the fire rate is less than the current fire time
        return !localData.Reloading && _timeSinceLastShoot > 1 / localData.fireRate / 60;
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
                onTriggerAttack.SetOwner(Owner);

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
    }

    public void SetOwner(GameObject owner)
    {
        Owner = owner;
    }
    public GameObject GetOwner()
    {
        return Owner;
    }
}
