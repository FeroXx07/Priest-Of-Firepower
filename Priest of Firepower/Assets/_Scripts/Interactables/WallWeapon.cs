using _Scripts.Interfaces;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.Interactables
{
    public class WallWeapon : MonoBehaviour, IInteractable
    {
        [SerializeField] string message;
        [SerializeField] float timeToInteract = 1f;
        [SerializeField] GameObject weapon;
        [SerializeField] int price;
        [SerializeField] InteractionPromptUI interactionPromptUI;
        private SpriteRenderer _wallWeaponImg;
        float _timer;
        public string Prompt => message;
        public float InteractionTime => timeToInteract;

        public int InteractionCost => price;

        private void OnEnable()
        {
            _timer = InteractionTime;
            Weapon.Weapon wp = weapon.GetComponent<Weapon.Weapon>(); 
            message = "Hold F to buy " + wp.weaponData.weaponName +" [" + wp.weaponData.price.ToString()+"]";
            interactionPromptUI.SetText(message);
            EnablePromptUI(false);
            _wallWeaponImg = GetComponent<SpriteRenderer>();
            _wallWeaponImg.sprite = wp.weaponData.sprite;
        }
        public void Interact(Interactor interactor, bool keyPressed)
        {
            if(keyPressed)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
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
                                _timer = InteractionTime;
                                EnablePromptUI(false);

                                pointSystem.RemovePoints(price);
                            }
                        }
                    }
                }
            }
            else
            {
                EnablePromptUI(true);
                _timer = timeToInteract;
            }
 
        }

        public void EnablePromptUI(bool show)
        {
            interactionPromptUI.gameObject.SetActive(show);
        }
    }
}
