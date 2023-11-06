using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPointsProvider 
{ 
    int ProvidePointsOnHit();
    int ProvidePointsOnDeath();
}
