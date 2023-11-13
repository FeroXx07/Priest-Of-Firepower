using System;
using UnityEngine;

namespace _Scripts.Interfaces
{
    public interface IDamageDealer
    {
        event Action<GameObject> onDamageDealerDestroyed;
        event Action<GameObject> onDamageDealth;

        int Damage { get; set; }
    }
}
