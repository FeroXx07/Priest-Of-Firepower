using System;
using _Scripts.Networking;
using TMPro;
using UnityEngine;

namespace _Scripts.Points.UI
{
    public class UIPoints : MonoBehaviour
    {
        [SerializeField] PointSystem pointSystem;
        [SerializeField] TMP_Text pointsTxt;
        private void SetPlayer(GameObject obj)
        {
            pointSystem = NetworkManager.Instance.player.GetComponent<PointSystem>();
            if(pointSystem != null) pointSystem.OnPointsChanged += UpdatePoints;
        }
        
        private void OnEnable()
        {
            NetworkManager.Instance.OnHostPlayerCreated += SetPlayer;
        }
        
        private void OnDisable()
        {
            NetworkManager.Instance.OnHostPlayerCreated -= SetPlayer;
            if(pointSystem != null) pointSystem.OnPointsChanged -= UpdatePoints;
        }

        void UpdatePoints(int points)
        {
            pointsTxt.text = points.ToString();
        }
    }
}
