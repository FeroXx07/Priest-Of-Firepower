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

    private void OnEnable()
    {
       
    }

    public void OnDamageableDestroyed()
    {
        onDamageableDestroyed?.Invoke(gameObject);
    }

    public void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner)
    {
        health -= damageDealer.Damage;
        onDamageTaken?.Invoke(gameObject);
        
        if(TryGetComponent<IPointsProvider>(out IPointsProvider pointsProvider ))
        {
            if (owner.TryGetComponent<PointSystem>(out PointSystem pointSystem))
            {
                pointSystem.PointsOnHit(pointsProvider);
            }
        }

        if (health <= 0)
        {
            if (TryGetComponent<IPointsProvider>(out IPointsProvider pointsProviders))
            {
                if (owner.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                {
                    pointSystem.PointsOnDeath(pointsProviders);
                }
            }
            
            health = 0;
            OnDamageableDestroyed();
        }
    }
}
