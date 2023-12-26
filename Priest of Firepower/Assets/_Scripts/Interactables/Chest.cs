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
        private int _weaponIndex = 0;
        private GameObject _weapon;
        private float _timer;
        public string Prompt => message;
        public bool IsBeingInteracted { get; }
        public float InteractionTime => time;
        public int InteractionCost => price;

        private InteractableState currentState = InteractableState.NONE;
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
            interactionPromptUI.SetText(message);
            GetComponent<SpriteRenderer>().sprite = sprites[0];
            EnablePromptUI(false);
            vfx.Stop();
            obtainedWeapon.SetActive(false);
            _timer = InteractionTime;
            _weaponReady = false;
            currentState = InteractableState.NONE;
        }

        public override void Update()
        {
            base.Update();

            if (NetworkManager.Instance.IsClient()) return;

            switch(state)
            {
                case InteractableState.INTERACTING:
                    
                    //open chest
                    if (!_openChest)
                    {
                        OpenChest();
                    }
                    //If chest showing the aviable weapon
                    //and player interact get weapon and close chest
                    else if(_weaponReady && _keyPressed && _interactorId != UInt64.MaxValue)
                    {
                        if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(interactorId,
                             out NetworkObject interactor))
                        {
                            if (_weapon != null)
                                interactor.GetComponent<WeaponSwitcher>().ChangeWeaponServer(_weapon);
                        }
                        CloseChest();
                    }
                    // Check if the weaponReady has been active for more than 9 seconds and close the chest.
                    if (_weaponReady && (Time.time - _weaponRuletteStartTime) >= 9)
                    {
                        CloseChest();
                    }

                    break;
                case InteractableState.INTERRUPTED:
                    _timer = InteractionTime;
                    interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
                    currentState = InteractableState.NONE;
                    break;
                case InteractableState.INTERACTION_COMPLETE:
                    _timer = InteractionTime;
                    _keyPressed = false;
                    _weaponIndex = 0;
                    _interactorId = UInt64.MaxValue;
                    _randomizingWeapon = false;
                    _openChest = false;
                    _weaponReady = false;
                    currentState = InteractableState.NONE;
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
            EnablePromptUI(false);

            _keyPressed = false;
            _interactorId = UInt64.MaxValue;
            if (NetworkManager.Instance.IsClient())
            {
                SendInputToServer();
            }
            else
            {
               SendReplicationData(ReplicationAction.UPDATE);
            }

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
            //no interaction allowed
            if (_randomizingWeapon) return;

            EnablePromptUI(true);

            interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);

            //if (interactor.GetComponent<PointSystem>().GetPoints() >= InteractionCost)
            //{
            //    interactionPromptUI.SetText("Not enough points!");
            //    return;
            //}

            
            if (keyPressed && (currentState == InteractableState.NONE || _weaponReady))
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _timer = InteractionTime;

                    _keyPressed = keyPressed;
                    _interactorId = interactor.GetObjId();

                    if (NetworkManager.Instance.IsClient())
                    {                        
                        interactor.GetComponent<PointSystem>().RemovePoints(InteractionCost);                       
                        _interactorId = interactor.GetObjId();
                        SendInputToServer();
                    }
                    else
                    {
                        currentState = InteractableState.INTERACTING;
                        _interactorId = interactor.GetObjId();
                        SendReplicationData(ReplicationAction.UPDATE);
                    }
                }
            }
            else
            {
                _timer = InteractionTime;
            }
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
                currentState = InteractableState.INTERACTING;

            if (!_keyPressed && currentState != InteractableState.INTERACTING)
                currentState = InteractableState.INTERRUPTED;

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
            writer.Write(_weaponIndex);

            ReplicationHeader header = new ReplicationHeader(GetObjId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return header;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {

            currentState = (InteractableState)reader.ReadInt32();
            Debug.Log("Receiving " + currentState);
            _interactorId = reader.ReadUInt64();
            _keyPressed = reader.ReadBoolean();
            _openChest = reader.ReadBoolean();
            _randomizingWeapon = reader.ReadBoolean();
            _weaponReady = reader.ReadBoolean();
            _weaponIndex = reader.ReadInt32();


            Debug.Log("Open " + _openChest);
            if (_openChest)
            {
                if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                {
                    spriteRenderer.sprite = sprites[1];
                }

                if (_randomizingWeapon)
                {
                    vfx.Play();
                }
                else
                {
                    vfx.Stop();
                }

                if (_weaponReady)
                {
                    _weapon = weapons[_weaponIndex];
                    obtainedWeapon.SetActive(true);
                    interactionPromptUI.SetText("F to pickup");
                    if (_weapon != null)
                    {
                        if (obtainedWeapon.TryGetComponent<SpriteRenderer>(out var weaponSpriteRenderer))
                        {
                            weaponSpriteRenderer.sprite = _weapon.GetComponent<Weapon.Weapon>().weaponData.sprite;
                        }
                    }
                }
            }
            else
            {
                if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                {
                    spriteRenderer.sprite = sprites[0];
                }

                obtainedWeapon.SetActive(false);
                interactionPromptUI.SetText(message);
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
            _weaponIndex = 0;

            obtainedWeapon.SetActive(false);

            _timer = InteractionTime * 2;

            currentState = InteractableState.INTERACTION_COMPLETE;

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
