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
    [SerializeField] InteractionPromptUI interactionPromptUI;
    private SpriteRenderer wallWeaponImg;
    float timer;
    public string Prompt => message;
    public float InteractionTime => timeToInteract;
    private void OnEnable()
    {
        timer = InteractionTime;
        Weapon wp = weapon.GetComponent<Weapon>(); 
        message = "Hold F to buy " + wp.weaponData._name +" [" + wp.weaponData.price.ToString()+"]";
        interactionPromptUI.Display(message);
        EnablePromptUI(false);
        wallWeaponImg = GetComponent<SpriteRenderer>();
        wallWeaponImg.sprite = wp.weaponData.sprite;
    }
    public void Interact(Interactor interactor)
    {
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            Debug.Log(Prompt);
            //TODO check update points

            // if has that weapon fill ammo 
            // if has a slot empty add to empty slot
            // if has not this weapon change by current weapon
            if(interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
            {
                switcher.ChangeWeapon(weapon);
                timer = InteractionTime;
                EnablePromptUI(false);
            }
        }
    }

    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }
}
