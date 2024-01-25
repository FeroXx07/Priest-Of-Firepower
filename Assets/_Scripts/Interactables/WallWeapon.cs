using System.IO;
using _Scripts.Interfaces;
using _Scripts.UI.Interactable;
using _Scripts.Weapon;
using UnityEngine;
using Unity.VisualScripting;
using _Scripts.Networking.Utility;
using _Scripts.Networking;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using _Scripts.Networking.Replication;

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
        private UInt64 _interactorId;
        InteractableState currentState;
        
        public InteractableState state
        {
            get => currentState;
            set => currentState = value;
        }

        public ulong interactorId
        {
            get => _interactorId;
            set => _interactorId = value;
        }

        public List<AudioClip> buySound;
        private AudioSource _audioSource;

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _timer = InteractionTime;
            Weapon.Weapon wp = weapon.GetComponent<Weapon.Weapon>();
            message = "Hold F to buy " + wp.weaponData.weaponName + "\n[costs: " + price + "]";
            interactionPromptUI.SetText(message);
            _wallWeaponImg = GetComponent<SpriteRenderer>();
            _wallWeaponImg.sprite = wp.weaponData.sprite;
            EnablePromptUI(false);
        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void Update()
        {
            base.Update();
        }

        public void Interact(Interactor interactor, bool keyPressed)
        {
            EnablePromptUI(true);
            interactionProgress.UpdateProgress(InteractionTime - _timer, InteractionTime);
            if (interactor.GetComponent<PointSystem>().GetPoints() < InteractionCost)
            {
                interactionPromptUI.SetText("Not enough points! [cost: " + price + "]");
                return;
            }
            else
            {
                interactionPromptUI.SetText(message);
            }

            if (keyPressed)
            {
                _interactorId = interactor.GetObjId();
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _timer = timeToInteract;
                    // if has that weapon fill ammo 
                    // if has a slot empty add to empty slot
                    // if has not this weapon change by current weapon
     
                    if (NetworkManager.Instance.IsClient())
                    {
                        SendInputToServer();
                        int sound = Random.Range(0, buySound.Count);
                        _audioSource.PlayOneShot(buySound[sound]);
                    }
                    else
                    {
                        if (interactor.TryGetComponent<WeaponSwitcher>(out WeaponSwitcher switcher))
                        {
                            int sound = Random.Range(0, buySound.Count);
                            _audioSource.PlayOneShot(buySound[sound]);
                            switcher.ChangeWeaponServer(weapon);         
                            interactor.GetComponent<PointSystem>().RemovePoints(price);
                        }
                    }
                    EnablePromptUI(false);
                }
            }
            else
            {
                _timer = timeToInteract;
            }
        }
        
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            if (showDebugInfo) Debug.Log($"Wall weapon: Sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            
            return true;
        }

        public override void SendInputToServer()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(_interactorId);
            SendInput(stream, true);
        }
        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            _interactorId = reader.ReadUInt64();

            if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(_interactorId,
                    out NetworkObject interactor))
            {
                WeaponSwitcher wp = interactor.GetComponent<WeaponSwitcher>();
                if(wp)
                    wp.ChangeWeaponServer(weapon);
                else
                {
                    Debug.Log("Could not change weapon");
                    return;
                }
            
                PointSystem ps = interactor.GetComponent<PointSystem>();
                if (ps)
                {
                    ps.RemovePoints(price); 
                }
                else
                {
                    Debug.Log("Could not remove points");
                    return;
                }
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

        protected override void InitNetworkVariablesList()
        {
        }
    }
}