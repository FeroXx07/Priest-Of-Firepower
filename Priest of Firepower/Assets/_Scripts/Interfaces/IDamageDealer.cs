using System;
using UnityEngine;

namespace _Scripts.Interfaces
{
    public interface IDamageDealer
    {
        event Action<GameObject> OnDamageDealerDestroyed;
        event Action<GameObject> OnDamageDealth;

        int Damage { get; set; }
    }
}
