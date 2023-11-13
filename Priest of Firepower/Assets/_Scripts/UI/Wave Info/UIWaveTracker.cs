using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using _Scripts.Enemies;

namespace _Scripts.UI.WaveInfo
{
    public class UIWaveTracker : MonoBehaviour
    {
        [SerializeField] RoundSystem roundSystem;
        [SerializeField] EnemyManager enemyManager;
        [SerializeField] TextMeshProUGUI waveCounter;
        [SerializeField] TextMeshProUGUI enemiesRemaining;

        private void OnEnable()
        {
            roundSystem.OnRoundBegin += UpdateWaveCounter;
            enemyManager.OnEnemyCountUpdate += UpdateEnemiesRemaining;
        }

        void UpdateWaveCounter(int waveCount)
        {
            waveCounter.text = "WAVE " + waveCount;
        }

        void UpdateEnemiesRemaining(int count)
        {
            enemiesRemaining.text = "Remaining: " + count;
        }

        
    }

}