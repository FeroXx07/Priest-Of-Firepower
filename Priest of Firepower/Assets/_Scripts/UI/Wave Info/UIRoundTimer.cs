using TMPro;
using UnityEngine;

namespace _Scripts.UI.Wave_Info
{
    public class UIRoundTimer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] RoundSystem roundSystem;

        float _timer = 0;

        private void OnEnable()
        {
            roundSystem.OnRoundEnd += StartTimer;
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;

                if (_timer > roundSystem.timeBetweenRounds / 4 * 3)

                {
                    titleText.text = "Round Finished!";
                    timerText.gameObject.SetActive(false);
                }
                else
                {
                    titleText.text = "Next Round in";
                    timerText.gameObject.SetActive(true);
                    SetText();

                }
            }
            else
            {
                _timer = 0;
                gameObject.SetActive(false);
                timerText.gameObject.SetActive(false);
            }

            

        }

        void StartTimer()
        {
            if (roundSystem != null)
            {
                gameObject.SetActive(true);
                _timer = roundSystem.timeBetweenRounds;
            }

        }

        void SetText()
        {
            timerText.text = Mathf.CeilToInt(_timer + 0.2f).ToString();
        }
    }
}
