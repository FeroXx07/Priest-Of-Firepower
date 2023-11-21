using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts
{
    public class WizardBulletMultiplication : MonoBehaviour
    {
        public float multiplicationTimer = 3.0f;
        public float angleOffset = 30.0f;
        public int multiplierCounter = 3;
        public GameObject objectCloned;

        private float _currentTimer = 0.0f;
        private Rigidbody2D _rb;
        private bool _isCloningScheduled = false;

        private void OnEnable()
        {
            _rb = GetComponent<Rigidbody2D>();
            _isCloningScheduled = false;
        }

        private void Update()
        {
            
            _currentTimer += Time.deltaTime;
            if (_currentTimer >= multiplicationTimer && !_isCloningScheduled)
            {
                _isCloningScheduled = true;  // Ensure cloning happens only once
                if (multiplierCounter > 0)
                {
                    CreateClones();
                    DisposeGameObject();
                }
                
            }
        }

        private void CreateClones()
        {
            Vector3 velocity = _rb.velocity; // Current velocity of the bullet
            float[] angles = { angleOffset, -angleOffset }; // Angles for the two clones

            foreach (float angle in angles)
            {
                Quaternion rotationOffset = Quaternion.Euler(0, 0, angle);
                GameObject clone = Instantiate(objectCloned, transform.position, rotationOffset * transform.rotation);

                Vector3 direction = Quaternion.Euler(0, 0, angle) * velocity;
                clone.GetComponent<Rigidbody2D>().velocity = direction;

                clone.GetComponent<WizardBulletMultiplication>().multiplierCounter--;
            }

        }

        private void DisposeGameObject()
        {
            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
