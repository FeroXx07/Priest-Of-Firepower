using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPointAwarder : MonoBehaviour, IPointsProvider
{

    [SerializeField] int pointsOnHit;
    [SerializeField] int pointsOnDeath;
    public int ProvidePointsOnDeath()
    {
        return pointsOnDeath;
    }

    public int ProvidePointsOnHit()
    {
        return pointsOnHit;
    }
}

