using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string _name;
    public int price;

    [Header("Shooting")]
    public float range;
    public int damage;
    public float fireRate;
    public float bulletSpeed;
    public float dispersion;

    [Header("Reloading")]
    public int ammoInMagazine;
    public int totalAmmo;
    public int maxAmmoCapacity;
    public int magazineSize;
    public float reloadSpeed;
    public bool Reloading;

    [Header("Visual")]
    public Sprite sprite;
}
