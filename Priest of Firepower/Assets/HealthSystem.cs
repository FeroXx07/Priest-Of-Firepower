using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour,IDamageable
{

    [SerializeField] private int health;
    [SerializeField] private int maxHealth;
    [SerializeField] private LayerMask layer;

    public LayerMask layers { get => layer; set => layer = value; }
    public int Health { get => health; set => health = value; }
    public int MaxHealth { get => maxHealth; set => maxHealth = value; }



    public event Action<GameObject> onDamageableDestroyed;
    public event Action<GameObject> onDamageTaken;

    public void OnDamageableDestroyed()
    {
        onDamageableDestroyed?.Invoke(gameObject);
    }

    public void TakeDamage(IDamageDealer damageDealer, Vector3 dir)
    {
        health -= damageDealer.Damage;
        onDamageTaken?.Invoke(gameObject);
    }

}
