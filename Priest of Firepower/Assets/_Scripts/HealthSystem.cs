using System;
using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts
{
    public class HealthSystem : MonoBehaviour,IDamageable
    {

        [SerializeField] private int health;
        [SerializeField] private int maxHealth;
        [SerializeField] private LayerMask layer;
        public LayerMask Layers { get => layer; set => layer = value; }
        public int Health { get => health; set => health = value; }
        public int MaxHealth { get => maxHealth; set => maxHealth = value; }

        public event Action<GameObject, GameObject> OnDamageableDestroyed;
        public event Action<GameObject, GameObject> OnDamageTaken;

        private void OnEnable()
        {
            health = maxHealth;
        }

        public void RaiseEventOnDamageableDestroyed(GameObject destroyer)
        {
            OnDamageableDestroyed?.Invoke(gameObject, destroyer);
        }

        public void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner)
        {
            health -= damageDealer.Damage;
            OnDamageTaken?.Invoke(gameObject, owner);

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
                RaiseEventOnDamageableDestroyed(owner);
            }
        }
    }
}
