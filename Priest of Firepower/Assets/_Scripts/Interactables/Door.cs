using System;
using System.Collections.Generic;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.UI.Interactables;
using Unity.VisualScripting;
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

        UInt64 interactorId; // client that is currently interacting
        float _timer;
        public string Prompt => message;

        public bool IsBeingInteracted { get; }
        public float InteractionTime => time;

        public int InteractionCost => price;

        private bool keyPressed = false;

        private InteractableState currentState;
        InteractableState IInteractable.state { get => currentState ; set => currentState = value; }

        bool _open = false;

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        protected override void InitNetworkVariablesList()
        {

        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);

            writer.Write((int)currentState);

            switch(currentState)
            {
                case InteractableState.INTERACTING:
                    writer.Write(interactorId);
                    break;
                case InteractableState.INTERRUPTED:
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    break;

                default:
                    break;
            }


            ReplicationHeader header = new ReplicationHeader(GetObjId(),this.GetType().FullName,ReplicationAction.UPDATE, outputMemoryStream.ToArray().Length);

            return header;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {

            InteractableState _state = (InteractableState)reader.ReadInt32();

            switch (_state)
            {
                case InteractableState.INTERACTING:

                    UInt64 newInteractorId =  reader.ReadUInt64();

                    //if no one is interacting store interaction
                    if(newInteractorId == UInt64.MaxValue)
                    {
                        interactorId = newInteractorId;
                    }
                    else
                    {

                    }


                    break;
                case InteractableState.INTERRUPTED:
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    break;
                default:
                    break;
            }

            return true;
        }

        public override void ReceiveInputFromClient(InputPacketHeader header, BinaryReader reader)
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
        private void Update()
        {

            if (isClient) return;

            //check wether the door can interacted with or not
            if (!CanInteract()) return;

            switch (currentState)
            {
                case InteractableState.INTERACTING:
                    {
                        _timer -= Time.deltaTime;
                        if (_timer <= 0)
                        {
                            _open = true;
                            Debug.Log(Prompt);
                            _timer = InteractionTime;
                            //pointSystem.RemovePoints(price);

                            EnablePromptUI(false);

                            EnableObjects(true);

                            DisableDoor();
                        }
                    }
                    break;

                case InteractableState.INTERRUPTED:
                    EnablePromptUI(true);
                    _timer = time;
                    interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
                    break;

                case InteractableState.INTERACTION_COMPLETE:
                    break;

                default:
                    break;

            }
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


        public override void SendInputToServer()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(keyPressed);
            SendInput(stream, false);
        }

        public void ClientInteract(Interactor interactor, bool _keyPressed)
        {
            if (keyPressed != _keyPressed)
            {
                MemoryStream stream = new MemoryStream();

                if (keyPressed)
                { 
                    currentState = InteractableState.INTERACTING; 
                    interactorId = interactor.GetObjId();
                }
                else
                { 
                    currentState = InteractableState.INTERRUPTED;
                    interactorId = UInt64.MaxValue;// no one
                }

                WriteReplicationPacket(stream, ReplicationAction.UPDATE);
                keyPressed = _keyPressed;
            }
           
        }
        public void ClientHandleInteraction(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void ServerInteract(Interactor interactor, bool _keyPressed)
        {
            keyPressed = _keyPressed;
        }

        public void ServerHandleInteraction(MemoryStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void Interact(State _state)
        {
            throw new System.NotImplementedException();
        }
    }
}
