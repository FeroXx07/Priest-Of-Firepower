using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    [SerializeField] float time;
    [SerializeField] InteractionPromptUI interactionPromptUI;
    [SerializeField] AudioClip audioClip;
    [SerializeField] VisualEffect vfx;
    [SerializeField] List<Sprite> sprites;
    [SerializeField] List<GameObject> weapons;
    [SerializeField] GameObject obtainedWeapon;
    GameObject weapon;
    float timer;
    public string Prompt => message;

    public float InteractionTime => time;


    bool randomizingWeapon;
    bool openChest;
    bool weaponReady;
    bool weaponRuletteActive = false;

    float weaponRuletteStartTime = 0f;
    private void OnEnable()
    {
        interactionPromptUI.SetText(message);
        GetComponent<SpriteRenderer>().sprite = sprites[0];
        EnablePromptUI(false);
        vfx.Stop();
        obtainedWeapon.SetActive(false);
        weaponReady = false;
    }

    private void Update()
    {
        // Check if the weapon rulette has been active for more than 9 seconds and close the chest.
        if (weaponReady && Time.time - weaponRuletteStartTime >= 9)
        {
            CloseChest();
            weaponRuletteActive = false;
        }
    }

    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }

    public void Interact(Interactor interactor, bool keyPressed)
    {

        if (randomizingWeapon) return;

        if(keyPressed)
        {
            //decrease timer to interact
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                //open chest
                if (!openChest)
                {
                    //TODO check update points
                    OpenChest();
                   
                }
                //If chest showing the aviable weapon
                //and player interact get weapon and close chest
                else
                {
                    if (interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                    {
                        Debug.Log("pickup weapon");
                        switcher.ChangeWeapon(weapon);
                        CloseChest();
                    }
                    else
                    {
                        Debug.Log("error geting weapon switcher");
                    }
                }

                timer = InteractionTime;
            }
        }
        else
        {
            EnablePromptUI(true);   
            timer = InteractionTime;
        }
    }
    private void OpenChest()
    {

        Debug.Log("Open");
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.sprite = sprites[1];
        }
        StartCoroutine(WeaponRulette());
        EnablePromptUI(false);
        openChest = true;
    }

    private void CloseChest()
    {
        Debug.Log("Close");
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.sprite = sprites[0];
        }
        StopCoroutine(WeaponRulette());
        EnablePromptUI(false);
        interactionPromptUI.SetText(message);
        randomizingWeapon = false;
        openChest = false;
        weaponReady = false;
        weapon = null;
        obtainedWeapon.SetActive(false);

        timer = InteractionTime * 2;
    }

    private IEnumerator WeaponRulette()
    {
        Debug.Log("random");
        vfx.Play();

        randomizingWeapon= true;

        weapon = GetRandomWeapon();

        if (obtainedWeapon.TryGetComponent<SpriteRenderer>(out var weaponSpriteRenderer))
        {
            weaponSpriteRenderer.sprite = weapon.GetComponent<Weapon>().weaponData.sprite;
        }

        yield return new WaitForSecondsRealtime(5);

        Debug.Log("weapon ready");

        vfx.Stop();

        randomizingWeapon = false;

        obtainedWeapon.SetActive(true);

        interactionPromptUI.SetText("F to Pickup");

        weaponReady = true;

        weaponRuletteStartTime = Time.time;
    }

    private GameObject GetRandomWeapon()
    {
        return weapons[Random.Range(0, weapons.Count)];
    }

}
