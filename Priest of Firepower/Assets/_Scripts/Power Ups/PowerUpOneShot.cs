using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerUpOneShot : PowerUpBase
{
    [SerializeField] private float powerUpTime = 10.0f;
    [SerializeField] private float timerCount = 0.0f;
    [SerializeField] bool isActive = false;
    List<Weapon> allWeapons = new List<Weapon>();
    public override void ApplyPowerUp()
    {
        base.ApplyPowerUp();

        // TODO: Make the next bullet of each player a one shot bullet. Change the damage for that bullet to 203574892357.
        allWeapons = FindObjectsOfType<Weapon>(true).ToList();
        allWeapons.ForEach(p => p.localData.damage = 10000);

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
                allWeapons.ForEach(p => p.localData.damage = p.weaponData.damage);
                allWeapons.Clear();
            }
        }
    }

}
