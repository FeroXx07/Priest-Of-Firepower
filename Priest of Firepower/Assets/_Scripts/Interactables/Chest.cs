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
    private void OnEnable()
    {
        interactionPromptUI.Display(message);
        GetComponent<SpriteRenderer>().sprite = sprites[0];
        EnablePromptUI(false);
        vfx.Stop();
        obtainedWeapon.SetActive(false);
    }

    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }

    public void Interact(Interactor interactor)
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            if(!openChest)
            {
                //TODO check update points
                timer = InteractionTime;

                Debug.Log("Open chest");

                OpenChest();
            }
            else
            {
                if (!randomizingWeapon && interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                {
                    
                    switcher.ChangeWeapon(weapon);
                    CloseChest();
                    timer = InteractionTime;                                     
                }
            }
        }
    }
    void OpenChest()
    {
        GetComponent<SpriteRenderer>().sprite = sprites[1];
        StartCoroutine(WeaponRulette());
        EnablePromptUI(false);
        openChest = true;
    }

    void CloseChest()
    {
        GetComponent<SpriteRenderer>().sprite = sprites[0];
        StopCoroutine(WeaponRulette());
        EnablePromptUI(false);
        interactionPromptUI.Display(message);
        randomizingWeapon = false;
        openChest = false;
        weapon = null;
        obtainedWeapon.SetActive(false);
    }

    IEnumerator WeaponRulette()
    {
        vfx.Play();

        randomizingWeapon= true;

        weapon = GetRandomWeapon();

        obtainedWeapon.GetComponent<SpriteRenderer>().sprite = weapon.GetComponent<Weapon>().data.sprite;

        yield return new WaitForSeconds(5);

        randomizingWeapon = false;

        vfx.Stop();

        interactionPromptUI.Display("F to Pickup");

        EnablePromptUI(true);

        obtainedWeapon.SetActive(true);

        yield return new WaitForSeconds(9);
        CloseChest();
      
    }

    GameObject GetRandomWeapon()
    {
        return weapons[Random.Range(0, weapons.Count)];
    }

}
