using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
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
    

        if(hit.collider!= null && Input.GetKeyDown(KeyCode.F))
        {

            interactable = hit.collider;
            IInteractable obj =  interactable.GetComponent<IInteractable>();

            obj.Interact(this);
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
