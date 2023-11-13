using UnityEngine;

namespace _Scripts.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float speed;
        Vector2 _direction;
        Rigidbody2D _rb;

        void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {

            _direction = Vector2.zero;
            if (Input.GetKey(KeyCode.A)) _direction += Vector2.left; 
            if (Input.GetKey(KeyCode.D)) _direction += Vector2.right;
            if (Input.GetKey(KeyCode.W)) _direction += Vector2.up;
            if (Input.GetKey(KeyCode.S)) _direction += Vector2.down;
            _direction.Normalize();

        }

        private void FixedUpdate()
        {
            _rb.velocity = _direction * speed;    
        }

    }
}
