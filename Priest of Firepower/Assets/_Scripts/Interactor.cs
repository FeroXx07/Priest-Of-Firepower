using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts
{
    public class Interactor : NetworkBehaviour
    {
        [SerializeField] KeyCode key;
        [SerializeField]private LayerMask layer;
        [SerializeField] private float interactionRange;
        private Collider2D _interactable;

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        protected override void InitNetworkVariablesList()
        {
     
        }

        public override void Update()
        {
            base.Update();
            // Get the mouse position in world coordinates.
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Vector3 dir = (mousePos - transform.position).normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir ,interactionRange,layer);    

            if(hit.collider != null)
            {
                _interactable = hit.collider;
                IInteractable obj =  _interactable.GetComponent<IInteractable>();
                //obj.Interact(this, Input.GetKey(key));
            }
            // if not looking any more the las interacteable, diable UI and clear the reference to it
            else if (_interactable != null)
            {
                IInteractable obj = _interactable.GetComponent<IInteractable>();
                obj.EnablePromptUI(false);
                _interactable = null;
            }
        
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Vector3 dir = (mousePos - transform.position).normalized;

            Gizmos.DrawLine(transform.position, transform.position + dir * interactionRange);
        }
    }
}
