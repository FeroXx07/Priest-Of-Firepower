using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        pointsTxt.text = "Score: " + points.ToString();
    }
}
