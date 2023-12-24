using UnityEngine;
using _Scripts.Enemies;
using UnityEngine.UI;

namespace _Scripts.UI.Enemies
{
    public class UISingleEnemyTracker : MonoBehaviour
    {
        public Enemy enemyToTrack;
        [SerializeField] UIEnemyOffLimitsTracker tracker;
        [SerializeField] float rotationSpeed;
        [SerializeField] float transparencyDistance;

        Image _sprite;

        private void Start()
        {
            _sprite = GetComponent<Image>();

            //Start facing the target
            Vector3 enemyPos = Camera.main.WorldToScreenPoint(enemyToTrack.gameObject.transform.position);
            transform.rotation = CalculateRotationToTarget(enemyPos);
        }

        void Update()
        {
            //pass world pos to screen pos
            Vector3 enemyPos = Camera.main.WorldToScreenPoint(enemyToTrack.gameObject.transform.position);

            transform.position = enemyPos;

            CheckScreenLimits();
            RotateToTarget(enemyPos);
            UpdateTransparency(enemyPos);
        }
        public void SetEnemy(UIEnemyOffLimitsTracker tracker, Enemy en)
        {
            enemyToTrack = en;
            this.tracker = tracker;
        }

        void CheckScreenLimits()
        {
            float _x = transform.position.x;
            float _y = transform.position.y;

            Vector3 trackerPos = tracker.gameObject.transform.position;

            //check right limit
            if (transform.position.x > trackerPos.x + tracker.screenOffsets.x)
            {
                _x = trackerPos.x + tracker.screenOffsets.x;
            }

            //check left limit
            if (transform.position.x < trackerPos.x - tracker.screenOffsets.x)
            {
                _x = trackerPos.x - tracker.screenOffsets.x;

            }

            //check up limit
            if (transform.position.y > trackerPos.y + tracker.screenOffsets.y)
            {
                _y = trackerPos.y + tracker.screenOffsets.y;
            }

            //check down limit
            if (transform.position.y < trackerPos.y - tracker.screenOffsets.y)
            {
                _y = trackerPos.y - tracker.screenOffsets.y;

            }

            Vector3 finalPos = new Vector3(_x, _y);
            transform.position = finalPos;

        }

        

        void RotateToTarget(Vector3 pos)
        {
            //Rotate gradually to target
            transform.rotation = Quaternion.Lerp(transform.rotation, CalculateRotationToTarget(pos), rotationSpeed);
        }

        Quaternion CalculateRotationToTarget(Vector3 pos)
        {
            Vector3 dir = (pos - transform.position).normalized;

            // Calculate the rotation angle in degrees.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Create a Quaternion for the rotation.
            return Quaternion.Euler(new Vector3(0f, 0f, angle + 90));
        }

        void UpdateTransparency(Vector3 pos)
        {
            if (_sprite == null) return;

            float dis = Vector3.Distance(pos, transform.position);

            float a = 1;
            if (dis < transparencyDistance)
            {
                a = Mathf.Lerp(0, 1, dis / transparencyDistance);
            }

            _sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b, a);
            
        }
    }
}
