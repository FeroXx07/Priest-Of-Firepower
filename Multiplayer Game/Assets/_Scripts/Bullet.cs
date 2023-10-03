using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour,IDamageDealer
{

    [SerializeField] LayerMask layers;
    int damage;
    public int Damage { get => damage; set => damage = value; }

    public LayerMask Layers { get =>layers; set => layers = value; }

    public event Action<GameObject> onDamageDealerDestroyed;
    public event Action<GameObject> onDamageDealth;

    bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;
    void CollisionHandeler(GameObject collision)
    {
        if (collision.TryGetComponent<IDamageable>(out IDamageable dmg))
        {
            if (IsSelected(collision.layer))
                dmg.TakeDamage(this, Vector2.zero);

            Destroy(gameObject);
        }
        if (IsSelected(collision.layer))
            Destroy(gameObject);

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisionHandeler(collision.gameObject);
    }
}
