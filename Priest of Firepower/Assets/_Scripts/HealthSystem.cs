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

    public event Action<GameObject, GameObject> onDamageableDestroyed;
    public event Action<GameObject, GameObject> onDamageTaken;

    private void OnEnable()
    {
        health = maxHealth;
    }

    public void OnDamageableDestroyed(GameObject destroyer)
    {
        onDamageableDestroyed?.Invoke(gameObject, destroyer);
    }

    public void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner)
    {
        health -= damageDealer.Damage;
        onDamageTaken?.Invoke(gameObject, owner);

        if (TryGetComponent<IPointsProvider>(out IPointsProvider pointsProvider ))
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
            OnDamageableDestroyed(owner);
        }
    }
}
