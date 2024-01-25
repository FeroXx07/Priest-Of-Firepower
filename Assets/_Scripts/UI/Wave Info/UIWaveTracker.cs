using _Scripts.Enemies;
using TMPro;
using UnityEngine;

namespace _Scripts.UI.Wave_Info
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