using System.IO;
using _Scripts.Interfaces;
using _Scripts.UI.Interactable;
using _Scripts.Weapon;
using UnityEngine;
using Unity.VisualScripting;
using _Scripts.Networking.Utility;
using _Scripts.Networking;

namespace _Scripts.Interactables
{
    public class WallWeapon : NetworkBehaviour, IInteractable
    {
        [SerializeField] string message;
        [SerializeField] float timeToInteract = 1f;
        [SerializeField] GameObject weapon;
        [SerializeField] int price;
        [SerializeField] InteractionPromptUI interactionPromptUI;
        [SerializeField] UIInteractionProgress interactionProgress;
        private SpriteRenderer _wallWeaponImg;
        float _timer;
        public string Prompt => message;
        public bool IsBeingInteracted { get; }
        public float InteractionTime => timeToInteract;

        public int InteractionCost => price;

        InteractableState currentState;
        public InteractableState state { get => currentState; set => currentState = value; }
        public ulong interactorId { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        public override void OnEnable()
        { 
            base.OnEnable();
            _timer = InteractionTime;
            Weapon.Weapon wp = weapon.GetComponent<Weapon.Weapon>(); 
            message = " " + wp.weaponData.weaponName +" [" + wp.weaponData.price.ToString()+"]";
            interactionPromptUI.SetText(message);
            EnablePromptUI(false);
            _wallWeaponImg = GetComponent<SpriteRenderer>();
            _wallWeaponImg.sprite = wp.weaponData.sprite;
        }
        public void Interact(Interactor interactor, bool keyPressed)
        {

            EnablePromptUI(true);

            interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);

            if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
            {
                interactionPromptUI.SetText("Not enough points!");
                return;
            }


            if (keyPressed)
            {
                
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                   // if has that weapon fill ammo 
                   // if has a slot empty add to empty slot
                   // if has not this weapon change by current weapon
                   if (interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                   {
                       switcher.ChangeWeaponServer(weapon);
                       _timer = InteractionTime;
                       EnablePromptUI(false);

                        interactor.GetComponent<PointSystem>().RemovePoints(price);
                   }
                    
                }
            }
            else
            {
                _timer = timeToInteract;
            }
     
        }

        public void EnablePromptUI(bool show)
        {
            interactionPromptUI.gameObject.SetActive(show);
        }

        public void InterruptInteraction()
        {
            EnablePromptUI(false);
        }

        public MemoryStream GetInteractionStream()
        {
            throw new System.NotImplementedException();
        }

        public void ReadInteractionStream(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void Interact(State _state)
        {
            throw new System.NotImplementedException();
        }

        public void ClientInteract(Interactor interactor, bool keyPressed)
        {
            throw new System.NotImplementedException();
        }

        public void ClientHandleInteraction(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void ServerInteract(Interactor interactor, bool keyPressed)
        {
            throw new System.NotImplementedException();
        }

        public void ServerHandleInteraction(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        protected override void InitNetworkVariablesList()
        {
            throw new System.NotImplementedException();
        }
    }
}
