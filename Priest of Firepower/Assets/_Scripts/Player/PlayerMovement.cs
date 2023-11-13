using UnityEngine;

namespace _Scripts.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float speed;
        Vector2 direction;
        Rigidbody2D rb;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {

            direction = Vector2.zero;
            if (Input.GetKey(KeyCode.A)) direction += Vector2.left; 
            if (Input.GetKey(KeyCode.D)) direction += Vector2.right;
            if (Input.GetKey(KeyCode.W)) direction += Vector2.up;
            if (Input.GetKey(KeyCode.S)) direction += Vector2.down;
            direction.Normalize();

        }

        private void FixedUpdate()
        {
            rb.velocity = direction * speed;    
        }

    }
}
