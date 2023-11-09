using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnTriggerAttack : Attack
{
    [SerializeField] protected bool destroyOnContact = true;
    protected virtual void CollisionHandeler(GameObject collision)
    {
        if (collision.TryGetComponent<IDamageable>(out IDamageable dmg))
        {
            if (IsSelected(collision.layer))
            {
                dmg.TakeDamage(this, Vector2.zero, owner);
                RaiseEventOnDealth(collision);
            }
        }

        if (IsSelected(collision.layer) && destroyOnContact)
            DisposeGameObject();
    }

    #region Collisions
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
    #endregion
}
