using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    [SerializeField] float timeToInteract = 1f;
    [SerializeField] GameObject weapon;
    [SerializeField] int cost;
    [SerializeField] GameObject uiElement;
    float timer;

    public string prompt => message;

    public float interactionTime => timeToInteract;


    private void OnEnable()
    {
        timer = interactionTime;
        EnableUIPrompt(false);
    }
    private void Update()
    {
        
    }
    public void Interact(Interactor interactor)
    {
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            //interact
            Debug.Log(prompt);
            //TODO check update points
            interactor.GetComponent<WeaponSwitcher>().ChangeWeapon(weapon);
            timer = interactionTime;
            EnableUIPrompt(false);
        }
    }
    public void EnableUIPrompt(bool show)
    {
        uiElement.SetActive(show);
    }
}
