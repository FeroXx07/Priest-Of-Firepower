using System;
using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts
{
    public class PointSystem : MonoBehaviour
    {
        private int _points;
        public int multiplyer = 1;
        public Action<int> OnPointsAdded;
        public Action<int> OnPointsRemoved;
        public Action<int> OnPointsChanged;

        void Start()
        {
        
            _points = 0;
        
            //Show points on start
            OnPointsChanged?.Invoke(_points);
        }

        public void AddPoints(int pointsToAdd)
        {
            _points += pointsToAdd;

            OnPointsAdded?.Invoke(pointsToAdd);
            OnPointsChanged?.Invoke(_points);
        }

        public void RemovePoints(int pointsToRemove)
        {
            _points -= pointsToRemove;
            OnPointsRemoved?.Invoke(pointsToRemove);
            OnPointsChanged?.Invoke(_points);
        }

        public int GetPoints() { return _points; }


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
