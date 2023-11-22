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

        private void Awake()
        {
            pointSystem = NetworkManager.Instance.player.GetComponent<PointSystem>();
        }
        
        private void OnEnable()
        {
            if(pointSystem != null) pointSystem.OnPointsChanged += UpdatePoints;
        }
        private void OnDisable()
        {
            if(pointSystem != null) pointSystem.OnPointsChanged -= UpdatePoints;
        }

        void UpdatePoints(int points)
        {
            pointsTxt.text = points.ToString();
        }
    }
}
