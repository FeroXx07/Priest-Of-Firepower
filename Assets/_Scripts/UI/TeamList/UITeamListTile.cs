using System;
using _Scripts.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.TeamList
{
    public class UITeamListTile : MonoBehaviour
    {
        [SerializeField] Image playerHealthBg;
        [SerializeField] Image playerHealth;
        [SerializeField] TextMeshProUGUI playerName;
        [SerializeField] TextMeshProUGUI playerPoints;
        [SerializeField] Gradient healthColor;
        [SerializeField] private Player.Player playerRef;
        public void Init(Player.Player player)
        {
            playerRef = player;

            playerRef.GetComponent<HealthSystem>().OnHealthChange += UpdatePlayerHealth;
            playerRef.GetComponent<PointSystem>().OnPointsChanged += UpdatePlayerPoints;
            
            playerName.SetText(playerRef.name);
        }

        private void OnDisable()
        {
            if (playerRef == null) return;
            playerRef.GetComponent<HealthSystem>().OnHealthChange -= UpdatePlayerHealth;
            playerRef.GetComponent<PointSystem>().OnPointsChanged -= UpdatePlayerPoints;
        }

        void UpdatePlayerHealth(int health, int maxHealth)
        {
            // ReSharper disable once PossibleLossOfFraction
            float amount = (float)health / (float)maxHealth;
            playerHealth.fillAmount = amount;
            playerHealth.color = healthColor.Evaluate(amount);

            float tmp = 0.4f;
            Color bgCol = playerHealth.color - new Color(tmp, tmp, tmp);
            bgCol.a = 1;
            playerHealthBg.color = bgCol;
        }
        
        void UpdatePlayerPoints(int ps)
        {
            playerPoints.text = ps.ToString();
        }
    }
}
