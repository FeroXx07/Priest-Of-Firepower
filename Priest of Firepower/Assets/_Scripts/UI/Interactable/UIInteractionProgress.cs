using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInteractionProgress : MonoBehaviour
{
    [SerializeField] Image sprite;

    public void UpdateProgress (float progress, float maxProgress)
    {
        sprite.fillAmount = progress / maxProgress;
    }
}
