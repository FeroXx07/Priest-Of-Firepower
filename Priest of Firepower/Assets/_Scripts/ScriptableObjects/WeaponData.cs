using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.ScriptableObjects
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [FormerlySerializedAs("name")] [FormerlySerializedAs("_name")] [Header("Info")]
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
        [FormerlySerializedAs("Reloading")] public bool reloading;

        [Header("Visual")]
        public Sprite sprite;
    }
}
