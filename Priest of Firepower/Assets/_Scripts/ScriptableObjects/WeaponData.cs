using System;
using System.Collections.Generic;
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
        public bool automatic;

        [Header("Reloading")]
        public int ammoInMagazine;
        public int totalAmmo;
        public int maxAmmoCapacity;
        public int magazineSize;
        public float reloadSpeed;
        public bool reloading;

        [Header("Visual")]
        public Sprite sprite;

        public float shakeIntensity;
        public List<AudioClip> shotSound;
        public List<AudioClip> reloadSound;
        
    }
}
