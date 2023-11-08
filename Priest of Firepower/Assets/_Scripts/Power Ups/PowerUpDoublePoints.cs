using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerUpDoublePoints : PowerUpBase
{
    [SerializeField] private float powerUpTime = 10.0f;
    [SerializeField] private float timerCount = 0.0f;
    [SerializeField] bool isActive = false;
    List<IPointsProvider> pointsProviders = new List<IPointsProvider>();
    public override void ApplyPowerUp()
    {
        base.ApplyPowerUp();

        // TODO: Give 2x points to all active players
        pointsProviders = FindObjectsOfType<MonoBehaviour>(true).OfType<IPointsProvider>().ToList();
        pointsProviders.ForEach(p => p.Multiplyer = 2);

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false); // Hide sprites
        }
        
        isActive = true;
    }

    private void Update()
    {
        Debug.Log($"isActive: {isActive}");
        if (isActive)
        {
            timerCount += Time.deltaTime;
            Debug.Log($"timerCount: {timerCount}");
            if (timerCount >= powerUpTime)
            {
                Debug.Log($"timer reached: {timerCount} >= {powerUpTime}");
                isActive = false;
                timerCount = 0.0f;
                pointsProviders.ForEach(p => p.Multiplyer = 1);
                Debug.Log($"points reseted");
                pointsProviders.Clear();
            }
        }
    }
}

