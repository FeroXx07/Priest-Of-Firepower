using _Scripts.Interfaces;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class OnTriggerAttack : Attack
    {
        [SerializeField] protected bool destroyOnContactWithLayer = true;
        [SerializeField] protected float destructionTime = 1.0f;
        private float timer;

        private void OnEnable()
        {
            timer = destructionTime;
        }
        private void Update()
        {
            timer -= Time.deltaTime;

            if (timer < 0.0f)
                DisposeGameObject();
        }
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

            if (IsSelected(collision.layer) && destroyOnContactWithLayer)
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
}
