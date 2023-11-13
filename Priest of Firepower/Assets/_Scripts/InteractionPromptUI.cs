using TMPro;
using UnityEngine;

namespace _Scripts
{
    public class InteractionPromptUI : MonoBehaviour
    {

        [SerializeField] TMP_Text promptText;
        public void Display()
        {
            gameObject.SetActive(true);
        }

        public void SetText(string prompt)
        {
            promptText.text = prompt;
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
