using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour,IDamageDealer
{

    [SerializeField] LayerMask layers;
    [SerializeField] int damage;
    public int Damage { get => damage; set => Damage = damage; }

    public LayerMask Layers { get =>layers; set => Layers = layers; }

    public event Action<GameObject> onDamageDealerDestroyed;
    public event Action<GameObject> onDamageDealth;

    bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;
    void CollisionHandeler(GameObject collision)
    {
        if (collision.TryGetComponent<IDamageable>(out IDamageable dmg))
        {
            if(IsSelected(layers))
                dmg.TakeDamage(this,Vector2.zero);
        }
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
