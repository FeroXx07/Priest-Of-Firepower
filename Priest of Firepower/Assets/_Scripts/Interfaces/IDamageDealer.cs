using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageDealer
{
    event Action<GameObject> onDamageDealerDestroyed;
    event Action<GameObject> onDamageDealth;

    int Damage { get; set; }
}
