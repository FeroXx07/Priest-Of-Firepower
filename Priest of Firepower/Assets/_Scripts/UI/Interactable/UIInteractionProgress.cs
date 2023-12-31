using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Interactable

{
    public class UIInteractionProgress : MonoBehaviour
    {
        [SerializeField] Image sprite;

        public void UpdateProgress(float progress, float maxProgress)
        {
            sprite.fillAmount = progress / maxProgress;
        }
    }
}
