using System;
using UnityEngine;

namespace _Scripts.ScriptableObjects
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Weapon Data")]
    [Serializable]
    public class WeaponData : ScriptableObject
    {
        [Header("Info")]
        public string weaponName;
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
        public bool reloading;

        [Header("Visual")]
        public Sprite sprite;
    }
}
