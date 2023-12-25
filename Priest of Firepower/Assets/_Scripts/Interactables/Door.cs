using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.UI.Interactable;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
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

        private InteractableState currentState = InteractableState.NONE;
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
            writer.Write(interactorId);
            writer.Write(keyPressed);
            
            
            switch(currentState)
            {
                case InteractableState.INTERACTING:
                    break;
                case InteractableState.INTERRUPTED:
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    break;
                case InteractableState.NONE:
                    break;
            }
            
            ReplicationHeader header = new ReplicationHeader(GetObjId(),this.GetType().FullName,action, outputMemoryStream.ToArray().Length);
            return header;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {

            InteractableState _state = (InteractableState)reader.ReadInt32();
            Debug.Log("Receiving " + currentState);
            interactorId = reader.ReadUInt64();
            keyPressed = reader.ReadBoolean();
            switch (_state)
            {
                case InteractableState.INTERACTING:
                    break;
                case InteractableState.INTERRUPTED:
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    DisableDoor();
                    break;
                case InteractableState.NONE :
                    break;
            }
            return true;
        }

        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            keyPressed = reader.ReadBoolean();
            interactorId = reader.ReadUInt64();
            
            if (keyPressed)
            {
                currentState = InteractableState.INTERACTING;
            }else
            {
                currentState = InteractableState.INTERRUPTED;
                interactorId = UInt64.MaxValue;
            }
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

        public void InterruptInteraction()
        {
             EnablePromptUI(false);
            // interactorId = UInt64.MaxValue;
            // keyPressed = false;
            // currentState = InteractableState.INTERRUPTED;
            // SendReplicationData(ReplicationAction.UPDATE);
        }

        public override void Update()
        {
            base.Update();
            
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
                            EnablePromptUI(false);
                            EnableObjects(true);
                            if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(interactorId,
                                    out NetworkObject interactor))
                            {
                                interactor.GetComponent<PointSystem>().RemovePoints(InteractionCost);
                            }
                            currentState = InteractableState.INTERACTION_COMPLETE;
                        }
                    }
                    break;

                case InteractableState.INTERRUPTED:
                    EnablePromptUI(true);
                    _timer = time;
                    interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
                    interactorId = UInt64.MaxValue;
                    currentState = InteractableState.NONE;
                    break;

                case InteractableState.INTERACTION_COMPLETE:
                    DisableDoor();
                    SendReplicationData(ReplicationAction.UPDATE);
                    break;
                case InteractableState.NONE:
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
            Destroy(this);
        }
        bool IsOpen() { return _open; }

        public override void SendInputToServer()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(keyPressed);
            writer.Write(interactorId);
            SendInput(stream, true);
        }

        public void Interact(Interactor interactor, bool _keyPressed)
        {
            interactionPromptUI.SetText(message);
            if (keyPressed != _keyPressed)
            {
                if (NetworkManager.Instance.IsClient())
                {
                    keyPressed = _keyPressed;
                    if (keyPressed)
                    {
                        if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
                        {
                            interactionPromptUI.SetText("Not enough points!");
                            return;
                        }
                        interactorId = interactor.GetObjId();
                    }
                    else
                        interactorId = UInt64.MaxValue;
                    
                    SendInputToServer();
                }
                else
                {
                    keyPressed = _keyPressed;

                    if (keyPressed)
                    {
                        if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
                        {
                            interactionPromptUI.SetText("Not enough points!");
                            return;
                        }
                        currentState = InteractableState.INTERACTING;
                        interactorId = interactor.GetObjId();
                    }else
                    {
                        currentState = InteractableState.INTERRUPTED;
                        interactorId = UInt64.MaxValue;
                    }
                    SendReplicationData(ReplicationAction.UPDATE);
                }
            }
            EnablePromptUI(true);
      }
    }
}
