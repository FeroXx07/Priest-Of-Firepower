using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointSystem : MonoBehaviour
{

    private int points;

    public Action<int> onPointsAdded;
    public Action<int> onPointsRemoved;

    public Action<int> onPointsChanged;

    void Start()
    {
        points = 0;
    }

    void AddPoints(int points_to_add)
    {
        points += points_to_add;

        onPointsAdded?.Invoke(points_to_add);
        onPointsChanged?.Invoke(points);
    }

    void RemovePoints(int points_to_remove)
    {
        points -= points_to_remove;
        onPointsRemoved?.Invoke(points_to_remove);
        onPointsChanged?.Invoke(points);
    }

    public int GetPoints() { return points; }


    public void PointsOnHit(IPointsProvider pointsProvider)
    {
        int points =  pointsProvider.ProvidePointsOnHit();

        AddPoints(points);
    }

    public void PointsOnDeath(IPointsProvider pointsProvider)
    {
        int points = pointsProvider.ProvidePointsOnDeath();

        AddPoints(points);
    }
}
