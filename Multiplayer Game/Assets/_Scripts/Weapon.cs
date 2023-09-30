using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData data;
    public GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    float timeSinceLastShoot;

    private void Start()
    {
        GetComponent<SpriteRenderer>().sprite = data.sprite;
    }
    private void OnEnable()
    {
        PlayerShooter.OnShoot += Shoot;
        PlayerShooter.OnReload += Reload;
        data.currentMaxAmmo = data.maxAmmo;
        data.currentAmmo = data.magazineSize;
    }

    void Reload()
    {
        if (data.Reloading || data.currentMaxAmmo <= 0 || data.currentAmmo >= data.magazineSize) return;

        StartCoroutine(Realoading());
    }
    IEnumerator Realoading()
    { 
        data.Reloading = true;

        yield return new WaitForSeconds(data.reloadSpeed);

        if(data.currentMaxAmmo > 0)
        {
            int bulletsToReload = (data.magazineSize - data.currentAmmo);

            data.currentMaxAmmo -= bulletsToReload;
            data.currentAmmo = data.magazineSize;
            data.Reloading = false;
        }

    }
    bool CanShoot()
    {
        //if is reloading or the fire rate is less than the current fire time
        return !data.Reloading && timeSinceLastShoot > 1 / data.fireRate / 60;
    }
    void Shoot(Vector2 direction)
    {

        if (data.currentAmmo > 0)
        {
            if (CanShoot())
            {
                GameObject bullet = Instantiate(bulletPrefab);
                bullet.transform.position = firePoint.position;
                bullet.GetComponent<Rigidbody2D>().velocity = direction * 5;

                data.currentAmmo--;
                timeSinceLastShoot = 0;
                OnGunShoot();
            }
        }
        else
        {
            Reload();
        }

    }
    private void Update()
    {
        timeSinceLastShoot += Time.deltaTime;
    }
    void OnGunShoot()
    {

    }
}
