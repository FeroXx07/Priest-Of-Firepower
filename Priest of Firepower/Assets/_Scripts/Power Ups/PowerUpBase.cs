using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpBase : MonoBehaviour
{
    public enum PowerUpType
    {
        MAX_AMMO,
        NUKE,
        DOUBLE_POINTS,
        ONE_SHOT
    }
    public PowerUpType type;
    public static Action<PowerUpType> powerUpPickedGlobal;
    public virtual void ApplyPowerUp() {
        if (coll2d)
            coll2d.enabled = false;
        powerUpPickedGlobal?.Invoke(type);
    }

    protected Collider2D coll2d;
    private void Awake()
    {
        coll2d = GetComponent<Collider2D>();
    }
}
