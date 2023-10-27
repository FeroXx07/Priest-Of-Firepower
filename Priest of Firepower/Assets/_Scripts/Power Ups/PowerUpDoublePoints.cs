using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpDoublePoints : PowerUpBase
{
    [SerializeField] private float powerUpTime = 10.0f;
    public override void ApplyPowerUp()
    {
        base.ApplyPowerUp();

        // TODO: Give 2x points to all active players
    }
}

