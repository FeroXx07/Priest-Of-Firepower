using System;
using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class NuclearBomb : MonoBehaviour, IDamageDealer
    {
        int damage = 10000;
        public int Damage { get => damage; set => damage = value; }

        public event Action<GameObject> onDamageDealerDestroyed;
        public event Action<GameObject> onDamageDealth;

        public void RaiseDamageDealthEvent(GameObject go)
        {
            onDamageDealth?.Invoke(go);
            onDamageDealerDestroyed?.Invoke(go);

            Destroy(this, 2.0f);
        }
    }
}
