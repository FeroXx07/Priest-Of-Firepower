using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class PowerUpDoublePoints : PowerUpBase
    {
        [SerializeField] private float powerUpTime = 10.0f;
        [SerializeField] private float timerCount = 0.0f;
        [SerializeField] bool isActive = false;
        List<PointSystem> _pointsProviders = new List<PointSystem>();
        public override void ApplyPowerUp()
        {
            base.ApplyPowerUp();

            // TODO: Give 2x points to all active players
            _pointsProviders = FindObjectsOfType<PointSystem>(true).ToList();
            _pointsProviders.ForEach(p => p.multiplier = 2);

            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false); // Hide sprites
            }
        
            isActive = true;
        }

        private void Update()
        {
            if (isActive)
            {
                timerCount += Time.deltaTime;
                if (timerCount >= powerUpTime)
                {
                    isActive = false;
                    timerCount = 0.0f;
                    _pointsProviders.ForEach(p => p.multiplier = 1);
                    _pointsProviders.Clear();
                }
            }
        }
    }
}

