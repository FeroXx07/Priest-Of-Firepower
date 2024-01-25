namespace _Scripts.Power_Ups
{
    public class PowerUpMaxAmmo : PowerUpBase
    {
        protected override void ApplyPowerUpServer()
        {
            Weapon.Weapon[] allWeapons = FindObjectsOfType<Weapon.Weapon>(true);
            foreach (Weapon.Weapon weapon in allWeapons)
            {
                if (weapon != null)
                    weapon.GiveMaxAmmo();
            }
        }
    }
}
