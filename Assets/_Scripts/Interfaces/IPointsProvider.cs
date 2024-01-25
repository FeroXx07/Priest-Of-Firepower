namespace _Scripts.Interfaces
{
    public interface IPointsProvider 
    { 
        int ProvidePointsOnHit();
        int ProvidePointsOnDeath();
        int PointsOnHit { get; }
        int PointsOnDeath { get; }
    }
}
