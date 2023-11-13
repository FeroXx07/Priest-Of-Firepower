using _Scripts.Enemies;

namespace _Scripts.Power_Ups
{
    public class PowerUpNuke : PowerUpBase
    {
        public override void ApplyPowerUp()
        {
            base.ApplyPowerUp();

            EnemyManager.Instance.KillAllEnemies();
        }
    }
}
