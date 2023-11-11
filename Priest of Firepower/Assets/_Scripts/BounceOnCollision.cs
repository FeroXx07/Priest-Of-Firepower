using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceOnCollision : MonoBehaviour
{
    #region Fields
    public int maxBounces = 3;
    public int reboundCounter = 0;
    #endregion
    private void OnEnable()
    {
        reboundCounter = 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        reboundCounter++;
        if (reboundCounter > maxBounces)
        {
            DisposeGameObject();
        }
      
        transform.right = GetComponent<Rigidbody2D>().velocity.normalized;
    }

    protected void DisposeGameObject()
    {
        if (TryGetComponent(out PoolObject pool))
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
