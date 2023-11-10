using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceOnCollision : MonoBehaviour
{
    #region Fields
    public int maxBounces = 3;
    public int reboundCounter = 0;
    public float destructionTime = 1.0f;
    private float timer;
    #endregion

    private void OnEnable()
    {
        timer = destructionTime;
        reboundCounter = 0;
    }
    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0.0f)
            DisposeGameObject();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        reboundCounter++;
        if (reboundCounter > maxBounces)
        {
            DisposeGameObject();
        }
        //transform.rotation = Quaternion.LookRotation(GetComponent<Rigidbody2D>().velocity.normalized, Vector3.forward);
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
