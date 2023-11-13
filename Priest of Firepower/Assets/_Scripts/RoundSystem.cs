using System;
using _Scripts.Enemies;
using UnityEngine;

namespace _Scripts
{
    public class RoundSystem : MonoBehaviour
    {
        private int _currentRound = 0;
        public float timeBetweenRounds = 5f;
        private float _timer;
        private bool _isCountDown =false;


        public Action<int> OnRoundBegin;
        public Action OnRoundEnd;

        public int GetCurrentRound() { return _currentRound; }
        public void StartRound()
        {
            _currentRound++;
            OnRoundBegin?.Invoke(_currentRound);
            Debug.Log("Round Started");
        }
        public void RoundFinished(EnemyManager enemyManager)
        {
            if (enemyManager.GetEnemiesCountLeft() <= 0 && enemyManager.GetEnemiesAlive() <= 0 && !_isCountDown)
            {
                Debug.Log("Round finished");
                OnRoundEnd?.Invoke();
                StartCountDown();
            }
        }
        private void Update()
        {
            if (_isCountDown)
            {
                _timer -= Time.deltaTime;

                if (_timer <= 0)
                {
                    StartRound();
                    _isCountDown = false;
                }
            }
        }

        private void StartCountDown()
        {
            _isCountDown = true;
            _timer = timeBetweenRounds;
            Debug.Log("Count down Started");
        }
    }
}