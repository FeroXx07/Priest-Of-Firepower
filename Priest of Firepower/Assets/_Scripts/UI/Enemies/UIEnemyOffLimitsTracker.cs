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
        [SerializeField] List<UISingleEnemyTracker> _enemyTrackers;
        private void OnEnable()
        {
            enemyManager.OnEnemySpawn += CreateTracker;
            enemyManager.OnEnemyRemove += DestroyTracker;
        }
        private void OnDisable()
        {
            enemyManager.OnEnemySpawn -= CreateTracker;
            enemyManager.OnEnemyRemove -= DestroyTracker;
        }
        private void Start()
        {
            _enemyTrackers = new List<UISingleEnemyTracker>();
            StartCoroutine(NullCheckRoutine());
        }

        IEnumerator NullCheckRoutine()
        {
            WaitForSeconds seconds = new WaitForSeconds(1);
            while (true)
            {
                foreach (UISingleEnemyTracker enemyTracker in _enemyTrackers)
                {
                    if (enemyTracker == null)
                        _enemyTrackers.Remove(enemyTracker);
                }

                yield return seconds;
            }
            yield return null;
        }
        void CreateTracker(Enemy e)
        {
            GameObject tracker = Instantiate(trackerRef, transform);
            UISingleEnemyTracker t = tracker.GetComponent<UISingleEnemyTracker>();

            t.SetEnemy(this, e);
            _enemyTrackers.Add(t);
        }
        void DestroyTracker(Enemy e)
        {
            GameObject toDestroy = null;

            foreach (UISingleEnemyTracker t in _enemyTrackers)
            {
                if (t.enemyToTrack == null)
                    _enemyTrackers.Remove(t);
                
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
