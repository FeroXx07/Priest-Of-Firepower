using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerHealth
{
    public class UIPlayerHealthTracker : MonoBehaviour
    {

        [SerializeField] HealthSystem health;
        [SerializeField] Image healthBar;
        [FormerlySerializedAs("healthBar_bg")] [SerializeField] Image healthBarBg;
        [SerializeField] Gradient healthColor;




        // Update is called once per frame
        void Update()
        {
            if (health.MaxHealth != 0)
            {
                healthBar.fillAmount = health.Health / (float)health.MaxHealth;

                healthBar.color = healthColor.Evaluate(healthBar.fillAmount);
                healthBarBg.color = new Color(healthBar.color.r, healthBar.color.g, healthBar.color.b, 0.5f);
            }
        }
    }
}
