using _Scripts.Networking;
using System;
using System.IO;
using _Scripts.Weapon;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Player
{
    public enum PlayerShooterInputs
    {
        SHOOT,
        RELOAD,
        NONE
    }
    public enum PlayerMovementInputs
    {
        UP = 0,
        RIGHT = 1,
        DOWN = 2,
        LEFT = 3
    }

    public enum PlayerState
    {
        MOVING,
        IDLE,
        INTERACTING,
        SHOOTING,
        RELOADING,
        DEAD
    }
    public class Player : NetworkBehaviour
    {
        [HideInInspector] public PlayerState state;

        #region User Data
        public void SetPlayerId(UInt64 id) => _playerId = id;
        public bool isOwner() => _playerId == NetworkManager.Instance.getId;
        [SerializeField] private bool isHost => NetworkManager.Instance.IsHost();
        [SerializeField] private bool isClient => NetworkManager.Instance.IsClient();
        public UInt64 GetPlayerId() => _playerId;
        [SerializeField] private UInt64 _playerId;
        [SerializeField] private UInt64 myUserId => NetworkManager.Instance.getId;
        public void SetName(string name) => _playerName = name;
        public string GetName() => _playerName;
        [SerializeField] private string _playerName;
        #endregion

        #region Movement
        [SerializeField] bool hasChangedMovement = false;
        [SerializeField] private Vector2 directionMovement;
        [SerializeField] private bool[] input = new bool[4];
        public float tickRatePlayer = 10.0f; // Network writes inside a second.
        [SerializeField] private float tickCounter = 0.0f;
        private Rigidbody2D _rb;
        public float speed = 7.0f;
        #endregion

        #region Shooter
        [SerializeField] private LineRenderer shootMarker;
        [SerializeField] private LayerMask layerMask;
        Transform _weaponHolder;
        [SerializeField] private float weaponOffset = .5f;
        [SerializeField] private Vector3 shootDir;
        [SerializeField] private PlayerShooterInputs currentWeaponInput;
        public Action OnShoot;
        public Action OnStartingReload;
        public Action OnReload;
        public Action OnFinishedReload;
        public Action<bool> OnFlip;
        [SerializeField] private bool _weaponFlipped;
        [SerializeField] private float _range = 1;
        [SerializeField] private Weapon.Weapon _currentWeapon; 
        [SerializeField] private WeaponSwitcher _weaponSwitcher;
        [SerializeField] private UInt64 myId => NetworkManager.Instance.getId;
        #endregion

        protected override void InitNetworkVariablesList()
        {
        }
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            
            input[0] = false; 
            input[1] = false; 
            input[2] = false; 
            input[3] = false;
            
            _weaponSwitcher = GetComponent<WeaponSwitcher>();
            _rb = GetComponent<Rigidbody2D>();
            state = PlayerState.IDLE;
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
        
        private void Start()
        {
            //if host render on top the player over the others
            if(isOwner())
                GetComponent<SpriteRenderer>().sortingOrder = 11;
            
            shootMarker.positionCount = 2;
            _weaponFlipped = false;
        }
        
        public override void Update()
        {
            base.Update();
            if (!isOwner()) return;  // Only the owner of the player will control it
            directionMovement = Vector2.zero;
            currentWeaponInput = PlayerShooterInputs.NONE;
            
            if (Input.GetMouseButton(0))
            {
                if (isHost)
                {
                    currentWeaponInput = PlayerShooterInputs.SHOOT;
                    OnShoot?.Invoke();
                    _currentWeapon.ShootServer();
                    SendInputToClients();
                }
                else if (isClient)
                {
                    currentWeaponInput = PlayerShooterInputs.SHOOT;
                    _currentWeapon.ShootClient();
                    SendInputToServer();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentWeaponInput = PlayerShooterInputs.RELOAD;
                OnReload?.Invoke();
            }
            
            if (Input.GetKey(KeyCode.W))
            {
                input[0] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.up;
            }
            else
            {
                input[0] = false;
            }

            if (Input.GetKey(KeyCode.D))
            {
                input[1] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.right;
            }
            else
            {
                input[1] = false;
            }

            if (Input.GetKey(KeyCode.S))
            {
                input[2] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.down;
            }
            else
            {
                input[2] = false;
            }

            if (Input.GetKey(KeyCode.A))
            {
                input[3] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.left;
            }
            else
            {
                input[3] = false;
            }
            
            bool isMoving = false;
            
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i])
                {
                    isMoving = true;
                    break;
                }   
            }
            
            if (isMoving)
            {
                state = PlayerState.MOVING;
                if (showDebugInfo) Debug.Log("is moving");
            }
            else
            {
                // if last state was moving and it stopped, then send that "stop" input
                if (state == PlayerState.MOVING)
                {
                    state = PlayerState.IDLE;
                    SendInputToServer();
                }
                state = PlayerState.IDLE;
            }
            
            if (hasChangedMovement)
            {
                hasChangedMovement = false;
                if (NetworkManager.Instance.IsClient())
                {    
                    Debug.Log("sending state" + state);
                    float finalRate = 1.0f / tickRatePlayer;
                    if (tickCounter >= finalRate)
                    {                      
                        tickCounter = 0.0f;
                        SendInputToServer();
                    }
                    tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;
                    tickCounter += Time.deltaTime;
                }
                // else
                // {
                //     Debug.Log("sending state" + state);
                //     float finalRate = 1.0f / tickRatePlayer;
                //     if (tickCounter >= finalRate)
                //     {
                //         tickCounter = 0.0f;
                //         SendInputToClients();
                //     }
                //     tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;
                //     tickCounter += Time.deltaTime;
                // }
            }
        }

        public void FixedUpdate()
        {
            //apply velocity to the client 
            _rb.velocity = directionMovement.normalized * speed;
            
            if (myId == _playerId)
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
            if (_weaponFlipped != flip)
            {
                _weaponFlipped = flip;
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
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            if (showDebugInfo) Debug.Log($"{_playerId} Player Shooter: Sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            float angle = reader.ReadSingle();
            shootDir.x = reader.ReadSingle();
            shootDir.y = reader.ReadSingle();
            if (showDebugInfo) Debug.Log($"{_playerId} Player Shooter: New angle received: {angle}");
            if (_weaponHolder != null)
                _weaponHolder.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            return true;
        }
        
        public override void SendInputToServer()
        {
            if (myUserId != _playerId)
                return;
            
            //Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Sending movement inputs TO server: {input}");
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);

            writer.Write(NetworkObject.GetNetworkId());
            
            writer.Write(_playerId);

            writer.Write((int)state);
            writer.Write((int)currentWeaponInput);
  
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
            
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }
        public override void ReceiveInputFromClient(BinaryReader reader)
        {
            Debug.Log($"{_playerId}--{_playerName}: Receiving movement inputs FROM client: {input}");

            UInt64 id = reader.ReadUInt64();

            state = (PlayerState)reader.ReadInt32();         
            currentWeaponInput = (PlayerShooterInputs)reader.ReadInt32();         
            
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            directionMovement = Vector2.zero;
            //store new direction
            if (input[0]) directionMovement += Vector2.up;
            if (input[1]) directionMovement += Vector2.right;
            if (input[2]) directionMovement += Vector2.down;
            if (input[3]) directionMovement += Vector2.left;

            if (currentWeaponInput == PlayerShooterInputs.SHOOT)
            {
                currentWeaponInput = PlayerShooterInputs.SHOOT;
                OnShoot?.Invoke();
                _currentWeapon.ShootServer();
                SendInputToClients();
            }
        }

        public override void SendInputToClients()
        {
            Debug.Log($"{_playerId}--{_playerName}: Sending movement inputs TO clients: {input}");
            // Redirect input to other clients
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write(_playerId);

            writer.Write((int)state);
            writer.Write((int)currentWeaponInput);

            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
            
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }

        public override void ReceiveInputFromServer(BinaryReader reader)
        {
            if (isOwner())
            {
                int remainingBytes = (sizeof(UInt64) + sizeof(Int32) * 2 + sizeof(bool) * 4);
                reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                return;
            }
            
            Debug.Log($"{_playerId}--{_playerName}: Receiving movement inputs FROM server: {input}");

            UInt64 id = reader.ReadUInt64();

            state = (PlayerState)reader.ReadInt32();
            currentWeaponInput = (PlayerShooterInputs)reader.ReadInt32();         

            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            directionMovement = Vector2.zero;
            
            if (input[0]) directionMovement += Vector2.up;
            if (input[1]) directionMovement += Vector2.right;
            if (input[2]) directionMovement += Vector2.down;
            if (input[3]) directionMovement += Vector2.left;
            
            if (currentWeaponInput == PlayerShooterInputs.SHOOT)
            {
                currentWeaponInput = PlayerShooterInputs.SHOOT;
                _currentWeapon.ShootClient();
                OnShoot?.Invoke();
            }
        }
    }
}