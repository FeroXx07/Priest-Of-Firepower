using System;
using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class NuclearBomb : MonoBehaviour, IDamageDealer
    {
        int _damage = 10000;
        public int Damage { get => _damage; set => _damage = value; }

        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;

        public void RaiseDamageDealthEvent(GameObject go)
        {
            OnDamageDealth?.Invoke(go);
            OnDamageDealerDestroyed?.Invoke(go);

            Destroy(this, 2.0f);
        }
    }
}
