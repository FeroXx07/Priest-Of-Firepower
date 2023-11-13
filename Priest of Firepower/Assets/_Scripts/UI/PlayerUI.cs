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
