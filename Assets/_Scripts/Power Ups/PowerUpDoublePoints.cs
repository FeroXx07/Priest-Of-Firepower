using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class PowerUpDoublePoints : PowerUpBase
    {
        
        List<PointSystem> _pointsProviders = new List<PointSystem>();
        protected override void ApplyPowerUpServer()
        {
            _pointsProviders = FindObjectsOfType<PointSystem>(true).ToList();
            _pointsProviders.ForEach(p => p.multiplier = 2);
            _pointsProviders.ForEach(p => p.SendData());
        }

        public override void Update()
        {
            base.Update();
            
            if (pickedUp)
            {
                powerUpCount += Time.deltaTime;
                if (powerUpCount >= powerUpTime)
                {
                    powerUpCount = 0.0f;
                    _pointsProviders.ForEach(p => p.multiplier = 1);
                    _pointsProviders.ForEach(p => p.SendData());
                    _pointsProviders.Clear();
                }
            }
        }
    }
}

