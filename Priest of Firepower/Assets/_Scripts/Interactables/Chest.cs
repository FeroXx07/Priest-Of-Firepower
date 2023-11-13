using System.Collections;
using System.Collections.Generic;
using _Scripts.Interfaces;
using _Scripts.Weapon;
using _Scripts.UI.Interactables;
using UnityEngine;
using UnityEngine.VFX;

namespace _Scripts.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] string message;
        [SerializeField] float time;
        [SerializeField] int price;
        [SerializeField] InteractionPromptUI interactionPromptUI;
        [SerializeField] UIInteractionProgress interactionProgress;
        [SerializeField] AudioClip audioClip;
        [SerializeField] VisualEffect vfx;
        [SerializeField] List<Sprite> sprites;
        [SerializeField] List<GameObject> weapons;
        [SerializeField] GameObject obtainedWeapon;
        GameObject _weapon;
        float _timer;
        public string Prompt => message;
        public float InteractionTime => time;
        public int InteractionCost => price;

        bool _randomizingWeapon;
        bool _openChest;
        bool _weaponReady;
        float _weaponRuletteStartTime = 0f;
        private void OnEnable()
        {
            interactionPromptUI.SetText(message);
            GetComponent<SpriteRenderer>().sprite = sprites[0];
            EnablePromptUI(false);
            vfx.Stop();
            obtainedWeapon.SetActive(false);
            _weaponReady = false;
        }

        private void Update()
        {
            // Check if the weaponReady has been active for more than 9 seconds and close the chest.
            if (_weaponReady && Time.time - _weaponRuletteStartTime >= 9)
            {
                CloseChest();
            }
        }

        public void EnablePromptUI(bool show)
        {
            interactionPromptUI.gameObject.SetActive(show);
        }

        public void Interact(Interactor interactor, bool keyPressed)
        {

            if (_randomizingWeapon) return;

            if(keyPressed)
            {
                //decrease timer to interact
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                
                    //open chest
                    if (!_openChest)
                    {
                        if(interactor.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                        {
                            if (pointSystem.GetPoints() >= InteractionCost)
                            {
                                OpenChest();
                                pointSystem.RemovePoints(price);
                            }
                        }                   
                    }
                    //If chest showing the aviable weapon
                    //and player interact get weapon and close chest
                    else
                    {
                        if (interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                        {
                            switcher.ChangeWeapon(_weapon);
                            CloseChest();
                        }
                        else
                        {
                            Debug.Log("error geting weapon switcher");
                        }
                    }

                    _timer = InteractionTime;
                }
            }
            else
            {
                EnablePromptUI(true);   
                _timer = InteractionTime;
            }
                interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
        }
        private void OpenChest()
        {
            if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.sprite = sprites[1];
            }
            StartCoroutine(WeaponRulette());
            EnablePromptUI(false);
            _openChest = true;
        }

        private void CloseChest()
        {
            if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.sprite = sprites[0];
            }
            StopCoroutine(WeaponRulette());
            EnablePromptUI(false);
            interactionPromptUI.SetText(message);
            _randomizingWeapon = false;
            _openChest = false;
            _weaponReady = false;
            _weapon = null;
            obtainedWeapon.SetActive(false);

            _timer = InteractionTime * 2;
        }

        private IEnumerator WeaponRulette()
        {
            vfx.Play();

            _randomizingWeapon= true;

            _weapon = GetRandomWeapon();

            if (obtainedWeapon.TryGetComponent<SpriteRenderer>(out var weaponSpriteRenderer))
            {
                weaponSpriteRenderer.sprite = _weapon.GetComponent<Weapon.Weapon>().weaponData.sprite;
            }

            yield return new WaitForSecondsRealtime(5);

            vfx.Stop();

            _randomizingWeapon = false;

            obtainedWeapon.SetActive(true);

            interactionPromptUI.SetText("F to Pickup");

            _weaponReady = true;

            _weaponRuletteStartTime = Time.time;
        }

        private GameObject GetRandomWeapon()
        {
            return weapons[Random.Range(0, weapons.Count)];
        }

    }
}
