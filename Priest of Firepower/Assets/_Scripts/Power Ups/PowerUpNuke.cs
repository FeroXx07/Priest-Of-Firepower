using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpNuke : PowerUpBase
{
    public override void ApplyPowerUp()
    {
        base.ApplyPowerUp();

        EnemyManager.Instance.KillAllEnemies();
    }
}
