using System;
using UnityEngine;

namespace _Scripts.Power_Ups
{
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
        public static Action<PowerUpType> PowerUpPickedGlobal;
        public virtual void ApplyPowerUp() {
            if (Coll2d)
                Coll2d.enabled = false;
            PowerUpPickedGlobal?.Invoke(type);
        }

        protected Collider2D Coll2d;
        private void Awake()
        {
            Coll2d = GetComponent<Collider2D>();
        }
    }
}
