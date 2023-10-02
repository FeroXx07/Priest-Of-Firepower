using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] KeyCode key;
    [SerializeField]private LayerMask layer;
    [SerializeField] private float interactionRange;
    private Collider2D interactable;
    private void Update()
    {
        // Get the mouse position in world coordinates.
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 dir = (mousePos - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir ,interactionRange,layer);
    

        if(hit.collider != null)
        {
            interactable = hit.collider;
            IInteractable obj =  interactable.GetComponent<IInteractable>();
            obj.EnableUIPrompt(true);
            if (Input.GetKey(key))
                obj.Interact(this);
        }
        // if not looking any more the las interacteable, diable UI and clear the reference to it
        else if (interactable != null)
        {
            IInteractable obj = interactable.GetComponent<IInteractable>();
            obj.EnableUIPrompt(false);
            interactable = null;
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
