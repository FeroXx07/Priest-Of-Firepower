using TMPro;
using UnityEngine;

namespace _Scripts.Points.UI
{
    public class UIPoints : MonoBehaviour
    {
        [SerializeField]PointSystem pointSystem;
        [SerializeField] TMP_Text pointsTxt;

        private void OnEnable()
        {
            pointSystem.OnPointsChanged += UpdatePoints;
        }
        private void OnDisable()
        {
            pointSystem.OnPointsChanged -= UpdatePoints;
        }

        void UpdatePoints(int points)
        {
            pointsTxt.text = points.ToString();
        }
    }
}
