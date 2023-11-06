using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    [SerializeField] float timeToInteract = 1f;
    [SerializeField] GameObject weapon;
    [SerializeField] int price;
    [SerializeField] InteractionPromptUI interactionPromptUI;
    private SpriteRenderer wallWeaponImg;
    float timer;
    public string Prompt => message;
    public float InteractionTime => timeToInteract;

    public int InteractionCost => price;

    private void OnEnable()
    {
        timer = InteractionTime;
        Weapon wp = weapon.GetComponent<Weapon>(); 
        message = "Hold F to buy " + wp.weaponData._name +" [" + wp.weaponData.price.ToString()+"]";
        interactionPromptUI.SetText(message);
        EnablePromptUI(false);
        wallWeaponImg = GetComponent<SpriteRenderer>();
        wallWeaponImg.sprite = wp.weaponData.sprite;
    }
    public void Interact(Interactor interactor, bool keyPressed)
    {
        if(keyPressed)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                if (interactor.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                {
                    if (pointSystem.GetPoints() >= InteractionCost)
                    {

                        // if has that weapon fill ammo 
                        // if has a slot empty add to empty slot
                        // if has not this weapon change by current weapon
                        if (interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                        {
                            switcher.ChangeWeapon(weapon);
                            timer = InteractionTime;
                            EnablePromptUI(false);

                            pointSystem.onPointsRemoved?.Invoke(price);
                        }
                    }
                }
            }
        }
        else
        {
            EnablePromptUI(true);
            timer = timeToInteract;
        }
 
    }

    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }
}
