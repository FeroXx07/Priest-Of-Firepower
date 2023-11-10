using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerHealthTracker : MonoBehaviour
{

    [SerializeField] HealthSystem health;
    [SerializeField] Image healthBar;
    [SerializeField] Image healthBar_bg;
    [SerializeField] Gradient healthColor;




    // Update is called once per frame
    void Update()
    {
        if (health.MaxHealth != 0)
        {
            healthBar.fillAmount = health.Health / (float)health.MaxHealth;

            healthBar.color = healthColor.Evaluate(healthBar.fillAmount);
            healthBar_bg.color = new Color(healthBar.color.r, healthBar.color.g, healthBar.color.b, 0.5f);
        }

        if (health.gameObject.transform.localScale.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);

        }
    }
}
