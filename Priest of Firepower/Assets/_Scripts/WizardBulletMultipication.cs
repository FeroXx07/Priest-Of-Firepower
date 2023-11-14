using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts
{
    public class WizardBulletMultiplication : MonoBehaviour
    {
        public float multiplicationTimer = 3.0f;
        public float angleOffset = 30.0f;
        public GameObject objectCloned;

        private float _currentTimer = 0.0f;
        private Rigidbody _rb;
        private bool _isCloningScheduled = false;

        private void OnEnable()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _currentTimer += Time.deltaTime;
            if (_currentTimer >= multiplicationTimer && !_isCloningScheduled)
            {
                CreateClones();
                DisposeGameObject();
                _isCloningScheduled = true;  // Ensure cloning happens only once
            }
        }

        private void CreateClones()
        {
            Vector3 velocity = _rb.velocity; // Current velocity of the bullet
            float[] angles = { angleOffset, -angleOffset }; // Angles for the two clones

            foreach (float angle in angles)
            {
                Quaternion rotationOffset = Quaternion.Euler(0, angle, 0);
                GameObject clone = Instantiate(objectCloned, transform.position, rotationOffset * transform.rotation);
                clone.GetComponent<Rigidbody>().velocity = rotationOffset * velocity;
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
