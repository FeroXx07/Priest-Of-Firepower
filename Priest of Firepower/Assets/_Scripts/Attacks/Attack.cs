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
        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;
        #endregion
        protected GameObject Owner;
        public void SetOwner(GameObject owner)
        {
            Owner = owner;
        }
        public GameObject GetOwner()
        {
            return Owner;
        }
        protected void DisposeGameObject()
        {
            OnDamageDealerDestroyed?.Invoke(gameObject);

            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
                Destroy(gameObject);
        }

        protected bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;

        protected void RaiseEventOnDestroyed(GameObject gameObject){
            OnDamageDealerDestroyed?.Invoke(gameObject);
        }
        protected void RaiseEventOnDealth(GameObject gameObject) {
            OnDamageDealth?.Invoke(gameObject);
        }
    }
}
