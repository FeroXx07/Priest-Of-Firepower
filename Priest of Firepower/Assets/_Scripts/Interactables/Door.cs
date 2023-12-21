using System.Collections.Generic;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.UI.Interactables;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Scripts.Interactables
{
    public class Door : NetworkBehaviour, IInteractable
    {

        [SerializeField] string message;
        [SerializeField] float time;
        [SerializeField] int price;
        [SerializeField] InteractionPromptUI interactionPromptUI;
        [SerializeField] UIInteractionProgress interactionProgress;
        [SerializeField] AudioClip audioClip;

        [SerializeField] List<Door> prerequisiteDoors;
        [SerializeField] List<GameObject> objectsToEnable;
        float _timer;
        public string Prompt => message;

        public bool IsBeingInteracted { get; }
        public float InteractionTime => time;

        public int InteractionCost => price;

        bool _open  = false;
        protected override void InitNetworkVariablesList()
        {
            
        }

        public override void OnEnable()
        {
            base.OnEnable();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            
            interactionPromptUI.SetText(message);
            EnablePromptUI(false);
            EnableObjects(false);
        }
        public void EnablePromptUI(bool show)
        {
            interactionPromptUI.gameObject.SetActive(show);
        }

        public MemoryStream GetInteractionStream()
        {
            throw new System.NotImplementedException();
        }

        public void ReadInteractionStream(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void Interact(Interactor interactor, bool keyPressed)
        {
            //check wether the door can interacted with or not
            if (!CanInteract()) return;

            if (keyPressed)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                { 
                    if (interactor.gameObject.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                    {
                        if (pointSystem.GetPoints() >= InteractionCost)
                        {
                            _open = true;
                            Debug.Log(Prompt);
                            _timer = InteractionTime;
                            pointSystem.RemovePoints(price);
                            
                            EnablePromptUI(false);

                            EnableObjects(true);

                            DisableDoor();
                            
                            
                            InteractionManager.Instance.ClientSendInteraction(interactor,this);
                        }
                    }
                }
            }
            else{
                EnablePromptUI(true);
                _timer = time;          
            }
                interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
        }
        private bool CanInteract()
        {
            bool canInteract = false;

            // if one of the prerequisite doors is open then enable door interaction
            if(prerequisiteDoors.Count > 0)
            {
                foreach (Door door in prerequisiteDoors)
                {
                    if (door.IsOpen())
                        canInteract = true;
                }
            }
            else
            {
                canInteract = true; 
            }


            return canInteract;
        }
        private void EnableObjects(bool enable)
        {
            foreach (var obj in objectsToEnable)
            {
                obj.SetActive(enable);
            }
        }

        private void DisableDoor()
        {
            GetComponent<BoxCollider2D>().enabled = false;
            GetComponent<ShadowCaster2D>().enabled = false;

            foreach(Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        bool IsOpen() { return _open; }
    }
}
