using TMPro;
using UnityEngine;

namespace _Scripts.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField]PointSystem pointSystem;
        [SerializeField] TMP_Text pointsTxt;

        private void OnEnable()
        {
            pointSystem.onPointsChanged += UpdatePoints;
        }
        private void OnDisable()
        {
            pointSystem.onPointsChanged -= UpdatePoints;
        }

        void UpdatePoints(int points)
        {
            pointsTxt.text = points.ToString();
        }
    }
}
