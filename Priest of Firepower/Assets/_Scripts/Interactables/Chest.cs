using System.Collections;
using System.Collections.Generic;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.UI.Interactable;
using _Scripts.Weapon;
using UnityEngine;
using UnityEngine.VFX;
using Unity.VisualScripting;
using System;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Enemies;
using _Scripts.Networking.Utility;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

namespace _Scripts.Interactables
{
    public class Chest : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string message;
        [SerializeField] private float time;
        [SerializeField] private int price;
        [SerializeField] private InteractionPromptUI interactionPromptUI;
        [SerializeField] private UIInteractionProgress interactionProgress;
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private VisualEffect vfx;
        [SerializeField] private List<Sprite> sprites;
        [SerializeField] private List<GameObject> weapons;
        [SerializeField] private GameObject obtainedWeapon;

        UInt64 _interactorId = UInt64.MaxValue;
        private bool _keyPressed = false;
        private int _spriteIndex;
        private int _weaponIndex;
        private GameObject _weapon;
        private float _timer;
        public string Prompt => message;
        public bool IsBeingInteracted { get; }
        public float InteractionTime => time;
        public int InteractionCost => price;

        private InteractableState currentState;
        public InteractableState state { get => currentState; set => currentState = value; }
        public ulong interactorId { get => _interactorId; set => _interactorId = value; }

        private bool _randomizingWeapon;
        private bool _openChest;
        private bool _weaponReady;
        private float _weaponRuletteStartTime = 0f;

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }
        public override void OnEnable()
        {
            base.OnEnable();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);

            interactionPromptUI.SetText(message);
            GetComponent<SpriteRenderer>().sprite = sprites[0];
            EnablePromptUI(false);
            vfx.Stop();
            obtainedWeapon.SetActive(false);
            _weaponReady = false;
        }

        public override void Update()
        {
            base.Update();
            

            switch(state)
            {
                case InteractableState.INTERACTING:
                    //decrease timer to interact
                    _timer -= Time.deltaTime;
                    if (_timer <= 0)
                    {
                        //open chest
                        if (!_openChest)
                        {
                            if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(interactorId,
                                 out NetworkObject interactor))
                            {
                                interactor.GetComponent<PointSystem>().RemovePoints(InteractionCost);
                            }
                            OpenChest();
                        }
                        //If chest showing the aviable weapon
                        //and player interact get weapon and close chest
                        else
                        {
                            if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(interactorId,
                                 out NetworkObject interactor))
                            {
                                if(_weapon != null)
                                    interactor.GetComponent<WeaponSwitcher>().ChangeWeaponServer(_weapon);
                            }
                            CloseChest();
                        }

                        _timer = InteractionTime;
                    }

                    // Check if the weaponReady has been active for more than 9 seconds and close the chest.
                    if (_weaponReady && Time.time - _weaponRuletteStartTime >= 9)
                    {
                        CloseChest();
                    }
                    break;
                case InteractableState.INTERRUPTED:
                    _timer = InteractionTime;
                    interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    break;
                    case InteractableState.NONE: break;
            }


        }

        public void EnablePromptUI(bool show)
        {
            interactionPromptUI.gameObject.SetActive(show);
        }

        public void InterruptInteraction()
        {
 
        }

        public MemoryStream GetInteractionStream()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);


            return stream;
        }

        public void ReadInteractionStream(MemoryStream stream)
        {

        }

        public void Interact(Interactor interactor, bool keyPressed)
        {

            if (_randomizingWeapon) return;

            _interactorId = interactor.GetObjId();
            if(keyPressed != _keyPressed)
            {
                _keyPressed = keyPressed;
                if(NetworkManager.Instance.IsClient())
                {
                    if(_keyPressed)
                    {
                        if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
                        {
                            interactionPromptUI.SetText("Not enough points!");
                            return;
                        }
                        _interactorId = interactor.GetObjId();
                    }
                    else
                    {
                        _interactorId = UInt64.MaxValue;
                    }
                    

                    SendInputToServer();
                }
                else
                {
                    if (keyPressed)
                    {
                        if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
                        {
                            interactionPromptUI.SetText("Not enough points!");
                            return;
                        }
                        currentState = InteractableState.INTERACTING;
                        _interactorId = interactor.GetObjId();
                    }
                    else
                    {
                        currentState = InteractableState.INTERRUPTED;
                        _interactorId = UInt64.MaxValue;
                    }
                    SendReplicationData(ReplicationAction.UPDATE);
                }
                
            }
            
            EnablePromptUI(true);   
     
        }

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

            if (_keyPressed)
            {
                currentState = InteractableState.INTERACTING;
            }
            else
            {
                currentState = InteractableState.INTERRUPTED;
                interactorId = UInt64.MaxValue;
            }
        }
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);

            writer.Write((int)currentState);
            writer.Write(_interactorId);
            writer.Write(_keyPressed);
            writer.Write(_openChest);
            writer.Write(_randomizingWeapon);
            writer.Write(_weaponReady);
            writer.Write(_spriteIndex);
            writer.Write(_weaponIndex);

            switch (currentState)
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

            ReplicationHeader header = new ReplicationHeader(GetObjId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return header;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {

            InteractableState _state = (InteractableState)reader.ReadInt32();
            Debug.Log("Receiving " + currentState);
            _interactorId = reader.ReadUInt64();
            _keyPressed = reader.ReadBoolean();
            _openChest = reader.ReadBoolean();
            _randomizingWeapon = reader.ReadBoolean();
            _weaponReady = reader.ReadBoolean();
            _spriteIndex = reader.ReadInt32();
            _weaponIndex = reader.ReadInt32();


            switch (_state)
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
            return true;
        }

        private void OpenChest()
        {
            if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.sprite = sprites[1];
            }

            EnablePromptUI(false);
            _openChest = true;

            StartCoroutine(WeaponRulette());
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

            SendReplicationData(ReplicationAction.UPDATE);
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

            SendReplicationData(ReplicationAction.UPDATE);

            yield return new WaitForSecondsRealtime(5);

            vfx.Stop();

            _randomizingWeapon = false;

            obtainedWeapon.SetActive(true);

            interactionPromptUI.SetText("F to Pickup");

            _weaponReady = true;

            _weaponRuletteStartTime = Time.time;

            SendReplicationData(ReplicationAction.UPDATE);
        }

        private GameObject GetRandomWeapon()
        {
            _weaponIndex = UnityEngine.Random.Range(0, weapons.Count);
            return weapons[_weaponIndex];
        }

        protected override void InitNetworkVariablesList()
        {
        }
    }
}
