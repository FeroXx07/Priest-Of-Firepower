using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts
{
    public class WizardBulletMultipication : MonoBehaviour
    {
        #region Fields

        public int numberOfCopies = 2;
        public float multiplicationTimer = 3.0f;
        public float angleOffset = 30.0f;
        public GameObject objectCloned;

        private float _currentTimer = 0.0f;
        private float _offsetDistance = 2.0f;
        private Rigidbody _rb;

        #endregion
        private void OnEnable()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _currentTimer += Time.deltaTime;
            if (_currentTimer >= multiplicationTimer)
            {
                Debug.Log("Create clones");
                InvokeRepeating("CreateClones", 0f, 0.5f);
                DisposeGameObject();
                _currentTimer = 0.0f;
            }
        }

        void CreateClones()
        {
            Vector3 velocity = _rb.velocity; // Current velocity

            // Calculate the angle increment based on the number of clones
            float angleIncrement = 360f / numberOfCopies;

            for (int i = 0; i < numberOfCopies; i++)
            {
                // Calculate the angle for this clone
                float angle = angleIncrement * i;

                // Calculate the position offset for this clone
                Vector3 offset = Quaternion.Euler(0, angle, 0) * transform.forward * _offsetDistance;

                // Instantiate the clone at the calculated position
                GameObject clone = Instantiate(objectCloned, transform.position + offset, transform.rotation);
                clone.GetComponent<Rigidbody>().velocity = velocity; // Apply the same velocity

                // If the clone has the BounceOnCollision component, set maxBounces to 0
                BounceOnCollision bounceComponent = clone.GetComponent<BounceOnCollision>();
                if (bounceComponent != null)
                {
                    bounceComponent.maxBounces = 0;
                }
            }

            CancelInvoke("CreateClones");
        }

        protected void DisposeGameObject()
        {
            if (TryGetComponent(out PoolObject pool))
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
    }
}
