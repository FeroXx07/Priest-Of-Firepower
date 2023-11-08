using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHealthTracker : MonoBehaviour
{


    UIProgressBar progressBar;
    HealthSystem healthSystem;



    // Start is called before the first frame update
    void Start()
    {
        healthSystem = transform.parent.gameObject.GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            progressBar.SetMaxValue(healthSystem.MaxHealth);
            healthSystem.onDamageTaken += UpdateProgressBar;

        }
    }

    void UpdateProgressBar(GameObject destroyed, GameObject destroyer)
    {
        progressBar.UpdateProgress(healthSystem.Health);
    }
}
