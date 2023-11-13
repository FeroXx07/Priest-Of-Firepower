using System;
using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts
{
    public class PointSystem : MonoBehaviour
    {
        private int points;
        public int multiplyer = 1;
        public Action<int> onPointsAdded;
        public Action<int> onPointsRemoved;
        public Action<int> onPointsChanged;

        void Start()
        {
        
            points = 0;
        
            //Show points on start
            onPointsChanged?.Invoke(points);
        }

        public void AddPoints(int points_to_add)
        {
            points += points_to_add;

            onPointsAdded?.Invoke(points_to_add);
            onPointsChanged?.Invoke(points);
        }

        public void RemovePoints(int points_to_remove)
        {
            points -= points_to_remove;
            onPointsRemoved?.Invoke(points_to_remove);
            onPointsChanged?.Invoke(points);
        }

        public int GetPoints() { return points; }


        public void PointsOnHit(IPointsProvider pointsProvider)
        {
            int points =  pointsProvider.ProvidePointsOnHit() * multiplyer;

            AddPoints(points);
        }

        public void PointsOnDeath(IPointsProvider pointsProvider)
        {
            int points = pointsProvider.ProvidePointsOnDeath() * multiplyer;

            AddPoints(points);
        }
    }
}
