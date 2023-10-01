using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageable
{
    event Action<GameObject> onDamageableDestroyed;
    event Action<GameObject> onDamageTaken;

    float Health { get; set; }    
    void TakeDamage(IDamageDealer damageDealer, Vector3 dir);
    void OnDamageableDestroyed();
}

