using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IDamageDealer
{
    #region Layers
    [SerializeField] LayerMask layers;
    public LayerMask Layers { get => layers; set => layers = value; }
    #endregion

    #region Damage
    public int damage;
    public int Damage { get => damage; set => damage = value; }
    public event Action<GameObject> onDamageDealerDestroyed;
    public event Action<GameObject> onDamageDealth;
    #endregion
    private void DisposeGameObject()
    {
        if (TryGetComponent(out PoolObject pool))
        {
            gameObject.SetActive(false);
        }
        else
            Destroy(gameObject);
    }
    void CollisionHandeler(GameObject collision)
    {
        if (collision.TryGetComponent<IDamageable>(out IDamageable dmg))
        {
            if (IsSelected(collision.layer))
                dmg.TakeDamage(this, Vector2.zero);

            DisposeGameObject();
        }
        if (IsSelected(collision.layer))
            DisposeGameObject();
    }

    #region Collisions
    bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
    #endregion
}