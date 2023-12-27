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
        UInt64 _interactorId; // client that is currently interacting
        float _timer;
        public string Prompt => message;

        public bool IsBeingInteracted { get; }
        public float InteractionTime => time;

        public int InteractionCost => price;

        private bool _keyPressed = false;

        private InteractableState currentState = InteractableState.NONE;
        InteractableState IInteractable.state { get => currentState ; set => currentState = value; }
        ulong IInteractable.interactorId { get => _interactorId; set => _interactorId = value; }

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
            writer.Write(_interactorId);
            writer.Write(_keyPressed);
            writer.Write(_open);

            ReplicationHeader header = new ReplicationHeader(GetObjId(),this.GetType().FullName,action, outputMemoryStream.ToArray().Length);
            return header;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {

            InteractableState _state = (InteractableState)reader.ReadInt32();
            Debug.Log("Receiving " + currentState);
            _interactorId = reader.ReadUInt64();
            _keyPressed = reader.ReadBoolean();
            _open = reader.ReadBoolean();

            if (_open)
                DisableDoor();

            return true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            message += " [" + price + "]";
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

            _open = true;

            if(!isClient)
                SendReplicationData(ReplicationAction.UPDATE);

            gameObject.SetActive(false);
        }
        bool IsOpen() { return _open; }

        public override void SendInputToServer()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(_keyPressed);
            writer.Write(_interactorId);
            SendInput(stream, true);
        }

        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            _keyPressed = reader.ReadBoolean();
            _interactorId = reader.ReadUInt64();
            DisableDoor();
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
            else
            {
                interactionPromptUI.SetText(message);
            }


            if (keyPressed && !_open)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _timer = InteractionTime;

                    _keyPressed = keyPressed;
                    _interactorId = interactor.GetObjId();

                    interactor.GetComponent<PointSystem>().RemovePoints(InteractionCost);

                    if (isClient)
                    {
                        SendInputToServer();
                    }
                    else
                    {
                        
                        DisableDoor();  
                    }
                }
            }
            else
            {
                _timer = InteractionTime;
            }
        }
    }
}
