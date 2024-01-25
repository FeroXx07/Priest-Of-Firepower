using System;
using UnityEngine;

namespace _Scripts.Interfaces
{
    public interface IDamageDealer
    {
        event Action<GameObject> OnDamageDealerDestroyed;
        event Action<GameObject> OnDamageDealth;
        int Damage { get; set; }
        void ProcessHit(IDamageable damageable, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject);
    }
}
