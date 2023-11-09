using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageable
{
    // The Action<Damaged object, Damager object>
    event Action<GameObject, GameObject> onDamageableDestroyed;
    event Action<GameObject, GameObject> onDamageTaken;

    LayerMask layers { get; set; }
    int Health { get; set; }    
    void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner);
    void OnDamageableDestroyed(GameObject destroyer);
}

