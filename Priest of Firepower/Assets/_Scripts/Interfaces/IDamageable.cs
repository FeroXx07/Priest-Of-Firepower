using System;
using UnityEngine;

namespace _Scripts.Interfaces
{
    public interface IDamageable
    {
        // The Action<Damaged object, Damager object>
        event Action<GameObject, GameObject> OnDamageableDestroyed;
        event Action<GameObject, GameObject> OnDamageTaken;

        LayerMask Layers { get; set; }
        int Health { get; set; }    
        void ProcessHit(IDamageDealer damageDealer, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject);
        void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner);
        void RaiseEventOnDamageableDestroyed(GameObject destroyer);
    }
}

