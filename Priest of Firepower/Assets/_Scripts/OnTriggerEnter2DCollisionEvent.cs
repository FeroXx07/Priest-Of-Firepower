using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEnter2DCollisionEvent : MonoBehaviour
{
    public UnityEvent<Collider2D> triggerEvent = new UnityEvent<Collider2D>();
    [SerializeField] LayerMask layers;

    [SerializeField] bool destroyGameObject = false;
    [SerializeField] float timeToDestroy = 0.2f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsSelected(collision.gameObject.layer))
            return;

        Debug.Log($"OnTriggerCollisionEvent + {gameObject.name} has collided with {collision.gameObject.name}");
        triggerEvent?.Invoke(collision);

        if (destroyGameObject)
            Destroy(gameObject, timeToDestroy);
    }

    bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;
}
