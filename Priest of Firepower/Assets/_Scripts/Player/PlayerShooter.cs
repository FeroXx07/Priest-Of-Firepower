using System;
using System.IO;
using _Scripts.Networking;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.Player
{
    public enum PlayerShooterInputs
    {
        SHOOT,
        RELOAD,
        NONE
    }
    
    public class PlayerShooter : NetworkBehaviour
    {
        [SerializeField] LineRenderer shootMarker;
        [SerializeField] LayerMask layerMask;
        Transform _weaponHolder;
        [SerializeField] float weaponOffset = .5f;
        [SerializeField] private Vector3 shootDir;
        [SerializeField] private PlayerShooterInputs currentInput;
        
        public Action OnShoot;
        public Action OnStartingReload;
        public Action OnReload;
        public Action OnFinishedReload;
        public Action<bool> OnFlip;
        
        [SerializeField] private bool _flipped;
        [SerializeField] private float _range = 1;
        
        [SerializeField] private Weapon.Weapon _currentWeapon; 
        [SerializeField] private Player _player;
        [SerializeField] private WeaponSwitcher _weaponSwitcher;
        [SerializeField] private UInt64 myId => NetworkManager.Instance.getId;
        
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            _weaponSwitcher = GetComponent<WeaponSwitcher>();
        }

        void Start()
        {
            shootMarker.positionCount = 2;
            _flipped = false;
            _player = GetComponent<Player>();
        }

        protected override void InitNetworkVariablesList()
        {
            
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            _weaponSwitcher.OnWeaponSwitch += ChangeHolder;
        }

        public override void  OnDisable()
        {
            base.OnDisable();
            _weaponSwitcher.OnWeaponSwitch -= ChangeHolder;
        }

        public override void Update()
        {
            base.Update();
            
            if (myId != _player.GetPlayerId())
                return;

            if (Input.GetMouseButton(0))
            {
                currentInput = PlayerShooterInputs.SHOOT;
                SendInputToServer();
                OnShoot?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentInput = PlayerShooterInputs.RELOAD;
                SendInputToServer();
                OnReload?.Invoke();
            }
            
            currentInput = PlayerShooterInputs.NONE;
        }

        private void FixedUpdate()
        {
            if (myId == _player.GetPlayerId())
            {
                // Get the mouse position in world coordinates.
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
            
                shootDir = (mousePos - transform.position).normalized;
            }
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, shootDir, _range, layerMask);
            if (hit)
            {
                UpdateShootMarker(hit.point);
            }
            else
            {
                Vector3 lineEnd = transform.position + shootDir * _range;

                UpdateShootMarker(lineEnd);
            }

            if (shootDir.x < 0)
                Flip(true);
            else
                Flip(false);


            // Calculate the rotation angle in degrees.
            float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

            // Create a Quaternion for the rotation.
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            
            _weaponHolder.transform.rotation = targetRotation;

            _weaponHolder.transform.position = transform.position + shootDir * weaponOffset;  
        }
        
        void UpdateShootMarker(Vector3 finalPos)
        {
            // Set the positions of the line _spriteRenderer.
            shootMarker.SetPosition(0, transform.position);
            shootMarker.SetPosition(1, finalPos);
        }

        void Flip(bool flip)
        {
            if (_flipped != flip)
            {
                _flipped = flip;
                OnFlip?.Invoke(flip);

                if (flip)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
            }
        }    

        void ChangeHolder(Transform holder)
        {
            _weaponHolder = holder;
            _currentWeapon = holder.GetComponentInChildren<Weapon.Weapon>();
            
            if (_currentWeapon != null)
            {
                _range = _currentWeapon.localData.range;
                _currentWeapon.SetPlayerShooter(this);
            }
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(_weaponHolder.transform.rotation.eulerAngles.z);
            writer.Write(shootDir.x);
            writer.Write(shootDir.y);
            writer.Write(shootDir.z);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            if (showDebugInfo) Debug.Log($"{_player.GetPlayerId()} Player Shooter: Sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            float angle = reader.ReadSingle();
            shootDir.x = reader.ReadSingle();
            shootDir.y = reader.ReadSingle();
            shootDir.z = reader.ReadSingle();
            if (showDebugInfo) Debug.Log($"{_player.GetPlayerId()} Player Shooter: New angle received: {angle}");
            _weaponHolder.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            return true;
        }

        public override void SendInputToServer()
        {
            if (myId != _player.GetPlayerId())
                return;
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write(_player.GetPlayerId());
            writer.Write((int)currentInput);
        }

        public override void ReceiveInputFromClient(BinaryReader reader)
        {
            UInt64 id = reader.ReadUInt64();
            currentInput = (PlayerShooterInputs)reader.ReadInt32();        
        }
    }
}

