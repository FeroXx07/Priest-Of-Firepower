using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Enemies;

namespace _Scripts.UI.Enemies
{
    public class UIEnemyOffLimitsTracker : MonoBehaviour
    {
        public Vector2 screenOffsets;
        [SerializeField] EnemyManager enemyManager;
        [SerializeField] GameObject trackerRef;
        List<UISingleEnemyTracker> _enemyTrackers;

        private void OnEnable()
        {
            enemyManager.OnEnemySpawn += CreateTracker;
            enemyManager.OnEnemyRemove += DestroyTracker;
        }

        private void Start()
        {
            _enemyTrackers = new List<UISingleEnemyTracker>();
        }
        void CreateTracker(Enemy e)
        {
            GameObject tracker = Instantiate(trackerRef);
            UISingleEnemyTracker t = tracker.GetComponent<UISingleEnemyTracker>();

            t.SetEnemy(this, e);
            tracker.transform.parent = transform;

            _enemyTrackers.Add(t);
        }

        void DestroyTracker(Enemy e)
        {
            GameObject toDestroy = null;

            foreach (UISingleEnemyTracker t in _enemyTrackers)
            {
                if (t.enemyToTrack == e)
                {
                    toDestroy = t.gameObject;
                    _enemyTrackers.Remove(t);
                    break;
                }
            }

            if (toDestroy != null) Destroy(toDestroy);
        }
    }
}
