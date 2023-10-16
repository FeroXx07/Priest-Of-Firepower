using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIProgressBar : MonoBehaviour
{
    [SerializeField]
    Sprite front;

    [SerializeField]
    Sprite background;

    public float maxValue;
    public float currentValue;

    float normalizedValue;
    
    public void UpdateProgress(float currentValue)
    {
        this.currentValue = currentValue;
        if (maxValue != 0)
        {
            normalizedValue = currentValue / maxValue;

            //show part of the front adapted to normalizedValue.
        }
    }

    public void SetMaxValue(float maxValue)
    {
        this.maxValue = maxValue;
        UpdateProgress(currentValue);
    }
}
