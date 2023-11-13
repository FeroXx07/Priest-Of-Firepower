using System;
using _Scripts.Interfaces;
using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class Attack : MonoBehaviour, IDamageDealer
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
        protected GameObject owner;
        public void SetOwner(GameObject owner_)
        {
            owner = owner_;
        }
        public GameObject GetOwner()
        {
            return owner;
        }
        protected void DisposeGameObject()
        {
            onDamageDealerDestroyed?.Invoke(gameObject);

            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
                Destroy(gameObject);
        }

        protected bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;

        protected void RaiseEventOnDestroyed(GameObject gameObject){
            onDamageDealerDestroyed?.Invoke(gameObject);
        }
        protected void RaiseEventOnDealth(GameObject gameObject) {
            onDamageDealth?.Invoke(gameObject);
        }
    }
}
