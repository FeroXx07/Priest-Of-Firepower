using _Scripts.Networking;
using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class Attack : NetworkBehaviour
    {
        [Header("Attack Properties")]
        #region Layers
        [SerializeField] LayerMask layers;
        public LayerMask Layers { get => layers; set => layers = value; }
        #endregion

        public int damage;
        protected GameObject owner;
        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
        }
        protected virtual void DisposeGameObject()
        {
            isDeSpawned = true;
            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
                Destroy(gameObject);
        }

        protected bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;
        
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }
        
        protected override void InitNetworkVariablesList()
        {
           
        }
    }
}
