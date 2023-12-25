using System;
using _Scripts.Networking;
using TMPro;
using UnityEngine;

namespace _Scripts.UI.Points
{
    public class UIPoints : MonoBehaviour
    {
        public Player.Player player;
        [SerializeField] PointSystem pointSystem;
        [SerializeField] TMP_Text pointsTxt;
        private void Init(GameObject obj)
        {
            FindAndSetPlayer();
            
            if (player == null)
            {
                Debug.LogError("No player instance!");
                return;
            }
            
            pointSystem = player.GetComponent<PointSystem>();
            if(pointSystem != null) pointSystem.OnPointsChanged += UpdatePoints;
        }

        void FindAndSetPlayer()
        {
            player = NetworkManager.Instance.player.GetComponent<Player.Player>();
        }
        
        private void OnEnable()
        {
            NetworkManager.Instance.OnHostPlayerCreated += Init;
        }
        
        private void OnDisable()
        {
            NetworkManager.Instance.OnHostPlayerCreated -= Init;
            if(pointSystem != null) pointSystem.OnPointsChanged -= UpdatePoints;
        }

        void UpdatePoints(int points)
        {
            pointsTxt.text = points.ToString();
        }
    }
}
