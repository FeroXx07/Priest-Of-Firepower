using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceOnCollision : MonoBehaviour
{
    #region Layers
    [SerializeField] LayerMask layersToIgnoreCollision;
    public LayerMask Layers { get => layersToIgnoreCollision; set => layersToIgnoreCollision = value; }
    protected bool IsSelectedIgnored(int layer) => ((layersToIgnoreCollision.value >> layer) & 1) == 1;
    #endregion

    #region Fields
    public int maxBounces = 3;
    public int reboundCounter = 0;
    public bool destroyAfterMaxBounces = true;
    public float destructionTime = 1.0f;
    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        reboundCounter++;
        if (reboundCounter > maxBounces)
        {
            DisposeGameObject();
        }
    }

    protected void DisposeGameObject()
    {
        if (TryGetComponent(out PoolObject pool))
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
