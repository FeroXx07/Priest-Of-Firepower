using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Scripts;

namespace _Scripts.UI.TeamInfo
{
    public class UITeamListTile : MonoBehaviour
    {

        [SerializeField] Image playerHealthBg;
        [SerializeField] Image playerHealth;
        [SerializeField] TextMeshProUGUI playerName;
        [SerializeField] TextMeshProUGUI playerPoints;
        [SerializeField] Gradient healthColor;
        [SerializeField] float testHealthAmount;


        private void OnEnable()
        {
            //subscribe UpdatePlayerHealth to specific OnPlayerHit
            //subscribe UpdatePlayerPoints to specific OnGetPoints
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        void UpdatePlayerHealth(HealthSystem hs)
        {
            float amount = hs.Health / hs.MaxHealth;
            playerHealth.fillAmount = amount;
            playerHealth.color = healthColor.Evaluate(amount);

            float tmp = 0.4f;
            Color bgCol = playerHealth.color - new Color(tmp, tmp, tmp);
            bgCol.a = 1;
            playerHealthBg.color = bgCol;

        }

        //void UpdatePlayerHealthTest(float n)
        //{
        //    float amount = n / 100;
        //    playerHealth.fillAmount = amount;
        //    playerHealth.color = healthColor.Evaluate(amount);

        //    float tmp = 0.4f;
        //    Color bgCol = playerHealth.color - new Color(tmp, tmp, tmp);
        //    bgCol.a = 1;
        //    playerHealthBg.color = bgCol;


        //}

        void UpdatePlayerPoints(PointSystem ps)
        {
            playerPoints.text = ps.GetPoints().ToString();
        }
    }
}
