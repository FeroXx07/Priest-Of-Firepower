using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    [SerializeField] float timeToInteract = 1f;
    [SerializeField] GameObject weapon;
    [SerializeField] int cost;
    [SerializeField] GameObject uiElement;
    float timer;
    public string Prompt => message;
    public float InteractionTime => timeToInteract;
    public GameObject UiElement { get => uiElement; set =>UiElement = value; }
    private void OnEnable()
    {
        timer = InteractionTime;
        UiElement.GetComponentInChildren<TMP_Text>().text = Prompt;
        EnableUIPrompt(false);
    }
    public void Interact(Interactor interactor)
    {
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            Debug.Log(Prompt);
            //TODO check update points
            interactor.GetComponent<WeaponSwitcher>().ChangeWeapon(weapon);
            timer = InteractionTime;
            EnableUIPrompt(false);
        }
    }
    public void EnableUIPrompt(bool show)
    {
        uiElement.SetActive(show);
    }
}
