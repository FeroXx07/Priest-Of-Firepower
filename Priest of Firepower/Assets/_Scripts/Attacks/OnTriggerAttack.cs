using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class OnTriggerAttack : Attack
    {
        [SerializeField] protected bool destroyOnContactWithLayer = true;
        [SerializeField] protected float destructionTime = 1.0f;
        private float _timer;

        private void OnEnable()
        {
            _timer = destructionTime;
        }
        private void Update()
        {
            _timer -= Time.deltaTime;

            if (_timer < 0.0f)
                DisposeGameObject();
        }
        protected virtual void CollisionHandeler(GameObject collision)
        {
            if (collision.TryGetComponent<IDamageable>(out IDamageable dmg))
            {
                if (IsSelected(collision.layer))
                {

                    if (Owner.TryGetComponent<NetworkObject>(out NetworkObject obj) &&
                        collision.TryGetComponent<NetworkObject>(out NetworkObject coll)&&
                        TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d)&&
                        TryGetComponent<NetworkObject>(out NetworkObject nObj))
                    {
                        if (obj == null) { Debug.Log(Owner.name + " has no network object"); return; }
                        if (coll == null) { Debug.Log(collision.name + " has no network object"); return; }
                        if (rb2d == null) { Debug.Log(name + " has no rb2d"); return; }
                        
                        HitManager.Instance.RegisterHit(obj.GetNetworkId(),
                                                        nObj.GetNetworkId(), 
                                                        coll.GetNetworkId(),
                                                        GetComponent<Collider2D>().isTrigger,
                                                        (Vector2)transform.position, 
                                                        GetComponent<Rigidbody2D>().velocity.normalized);


                    }
                    
                    //dmg.TakeDamage(this, Vector2.zero, Owner);

                    //RaiseEventOnDealth(collision);
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
