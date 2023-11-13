namespace _Scripts.Power_Ups
{
    public class PowerUpMaxAmmo : PowerUpBase
    {
        public override void ApplyPowerUp()
        {
            base.ApplyPowerUp();

            Weapon.Weapon[] allWeapons = FindObjectsOfType<Weapon.Weapon>();
            foreach (Weapon.Weapon weapon in allWeapons)
            {
                if (weapon != null)
                    weapon.GiveMaxAmmo();
            }

        }
    }
}
