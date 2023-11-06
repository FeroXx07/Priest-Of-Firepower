using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointSystem : MonoBehaviour
{

    private int points;

    public Action<int> onPointsAdded;
    public Action<int> onPointsRemoved;

    void Start()
    {
        points = 0;
    }

    void AddPoints(int points_to_add)
    {
        points += points_to_add;

        onPointsAdded(points_to_add);
    }

    void RemovePoints(int points_to_remove)
    {
        points -= points_to_remove;
        onPointsRemoved(points_to_remove);
    }

    public int GetPoints() { return points; }
}
