using _Scripts.Networking;
using System;
using System.Collections;
using System.IO;
using System.Xml;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.ScriptableObjects;
using _Scripts.Weapon;
using Cinemachine;
using UnityEngine;

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
        public UInt64 GetPlayerId() => _playerId;
        [SerializeField] private UInt64 _playerId;
        [SerializeField] private UInt64 myUserId => NetworkManager.Instance.getId;
        public void SetName(string name) => _playerName = name;
        public string GetName() => _playerName;
        [SerializeField] private string _playerName;
        private SpriteRenderer _spriteRenderer;
        #endregion

        #region Player State

        public Action OnPlayerDeath;
        private HealthSystem _healthSystem;
        

        #endregion
        #region Movement
        [SerializeField] bool hasChangedMovement = false;
        [SerializeField] private Vector2 directionMovement;
        [SerializeField] private bool[] input = new bool[4];
        public float tickRatePlayer = 10.0f; // Network writes inside a second.
        [SerializeField] private float tickCounter = 0.0f;
        private Rigidbody2D _rb;
        public float speed = 7.0f;
        public bool isParalized = false;
        #endregion

        #region Shooter
        [SerializeField] private LayerMask layerMask;
        Transform _weaponHolder;
        [SerializeField] private float weaponOffset = .5f;
        [SerializeField] private Vector3 shootDir;
        [SerializeField] private PlayerShooterInputs currentWeaponInput;
        public Action<WeaponData> OnShoot;
        public Action<WeaponData> OnStartingReload;
        public Action<WeaponData> OnReload;
        public Action<WeaponData> OnFinishedReload;
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
            
            GetComponent<NetworkObject>().speed = speed;
            
            clientSendReplicationData = true;

            _healthSystem = GetComponent<HealthSystem>();

        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            _weaponSwitcher.OnWeaponSwitch += ChangeHolder;
            _healthSystem.OnDamageableDestroyed += Die;
        }

        public override void OnDisable()
        {
            Debug.LogWarning($"{GetName()}: OnDisable()");
            base.OnDisable();
            _weaponSwitcher.OnWeaponSwitch -= ChangeHolder;
            _healthSystem.OnDamageableDestroyed -= Die;
        }
        
        private void Start()
        {
            Debug.Log($"{GetName()}: Initiating player and UI");
            NetworkManager.Instance.AnyPlayerCreated(gameObject);
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (isOwner())
            {
                Debug.Log($"{GetName()}: Initiating host player and UI");
                GetComponent<SpriteRenderer>().sortingOrder = 13;
                FindObjectOfType<CinemachineVirtualCamera>().Follow = transform;
                NetworkManager.Instance.player = gameObject;
                NetworkManager.Instance.OwnerPlayerCreated(gameObject);
                //GameManager.Instance.StartGame();
            }
            StartCoroutine(LateStart());
        }

        IEnumerator LateStart()
        {
            yield return new WaitForSeconds(0.2f);
            _weaponSwitcher.InitializeWeapons();
        }
        
        public override void Update()
        {
            base.Update();
            if (!isOwner()) return;  // Only the owner of the player will control it
            
            if(state == PlayerState.DEAD)
                return;            
            
            directionMovement = Vector2.zero;
            currentWeaponInput = PlayerShooterInputs.NONE;

            if (_currentWeapon != null)
            {
                if (_currentWeapon.weaponData.automatic)
                {
                    // Automatic weapon
                    if (Input.GetMouseButton(0))
                    {
                        currentWeaponInput = PlayerShooterInputs.SHOOT;
                        OnShoot?.Invoke(_currentWeapon.localData);
                        if (isHost)
                        {
                            _currentWeapon.ShootServer();
                            SendInputToClients();
                        }
                        else if (isClient)
                        {
                            _currentWeapon.ShootClient();
                            SendInputToServer();
                        }
                    }
                }
                else
                {
                    // Semi-automatic
                    if (Input.GetMouseButtonDown(0))
                    {
                        currentWeaponInput = PlayerShooterInputs.SHOOT;
                        OnShoot?.Invoke(_currentWeapon.localData);
                        if (isHost)
                        {
                            _currentWeapon.ShootServer();
                            SendInputToClients();
                        }
                        else if (isClient)
                        {
                            _currentWeapon.ShootClient();
                            SendInputToServer();
                        }
                    }
                }
                
                if (Input.GetKeyDown(KeyCode.R))
                {
                    currentWeaponInput = PlayerShooterInputs.RELOAD;
                    _currentWeapon.Reload();
                    OnReload?.Invoke(_currentWeapon.localData);
                    if (isHost)
                    {
                        SendInputToClients();
                    }
                    else if (isClient)
                    {
                        SendInputToServer();
                    }
                }
            }

            if (Input.GetKey(KeyCode.W))
            {
                input[0] = true;
                hasChangedMovement = true;
                if ( !isParalized) directionMovement += Vector2.up;
            }
            else
            {
                input[0] = false;
            }

            if (Input.GetKey(KeyCode.D))
            {
                input[1] = true;
                hasChangedMovement = true;
                if ( !isParalized) directionMovement += Vector2.right;
            }
            else
            {
                input[1] = false;
            }

            if (Input.GetKey(KeyCode.S))
            {
                input[2] = true;
                hasChangedMovement = true;
                if ( !isParalized) directionMovement += Vector2.down;
            }
            else
            {
                input[2] = false;
            }

            if (Input.GetKey(KeyCode.A))
            {
                input[3] = true;
                hasChangedMovement = true;
                if ( !isParalized) directionMovement += Vector2.left;
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
                if (showDebugInfo) Debug.Log($"{GetName()}: Is moving");
            }
            else
            {
                // if last state was moving and it stopped, then send that "stop" input
                if (state == PlayerState.MOVING)
                {
                    state = PlayerState.IDLE;
                    SendInputToServer();
                    NetworkObject.WriteReplicationTransform(TransformAction.INTERPOLATE);
                }
                state = PlayerState.IDLE;
            }
            
            if (hasChangedMovement || isParalized)
            {
                hasChangedMovement = false;
                if (NetworkManager.Instance.IsClient())
                {    
                    if (showDebugInfo) Debug.Log($"{GetName()}: Sending state {state}");
                    float finalRate = 1.0f / tickRatePlayer;
                    if (tickCounter >= finalRate)
                    {                      
                        tickCounter = 0.0f;
                        SendInputToServer();
                    }
                    tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;
                    tickCounter += Time.deltaTime;
                }
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
            
            if (shootDir.x < 0)
                Flip(true);
            else
                Flip(false);


            // Calculate the rotation angle in degrees.
            float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

            // Create a Quaternion for the rotation.
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));

            if (_weaponHolder != null)
            {
                var transform1 = _weaponHolder.transform;
                transform1.rotation = targetRotation;
                transform1.position = transform.position + (shootDir * weaponOffset);  
            }
        }
        void Flip(bool flip)
        {           
            _spriteRenderer.flipX = flip;

            if(_currentWeapon != null)
                _currentWeapon.FlipGun(flip);
        }    

        void ChangeHolder(Transform holder)
        {
            _weaponHolder = holder;
            _currentWeapon = null;
            _currentWeapon = holder.GetComponentInChildren<Weapon.Weapon>();
                        
            if (_currentWeapon != null)
            {
                if(isOwner())
                    _currentWeapon.GetComponent<SpriteRenderer>().sortingOrder = 15;
                _currentWeapon.SetPlayerShooter(this);
            }
        }

        void Die(GameObject self,GameObject killer)
        {
            Debug.Log($"{GetName()}: Dying");
            state = PlayerState.DEAD;
            OnPlayerDeath?.Invoke();
            SendReplicationData(ReplicationAction.UPDATE);
            
            if(isHost)
                GameManager.Instance.CheckGameOver();
        }

        public void SetParalize(bool value)
        {
            isParalized = value;
            if (isHost)
            { 
                SendInputToClients();
            }
            else if (isClient)
            {
                SendInputToServer();
            }
        }
        
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            writer.Write((int)state);
            
            if (_weaponHolder == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(_weaponHolder.transform.rotation.eulerAngles.z);
                writer.Write(shootDir.x);
                writer.Write(shootDir.y);
            }
            writer.Write(isParalized);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            if (showDebugInfo) Debug.Log($"{_playerId} Player Shooter: Sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            state = (PlayerState)reader.ReadInt32();
            //check when a player dies if other players are still alive to call a game over
            if (state == PlayerState.DEAD && isHost)
            {
                GameManager.Instance.CheckGameOver();
            }
                
            if (reader.ReadBoolean())
            {
                float angle = reader.ReadSingle();
                shootDir.x = reader.ReadSingle();
                shootDir.y = reader.ReadSingle();
                if (showDebugInfo) Debug.Log($"{_playerId} Player Shooter: New angle received: {angle}");
                if (_weaponHolder != null)
                    _weaponHolder.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            }

            isParalized = reader.ReadBoolean();
            return true;
        }
        
        public override void SendInputToServer()
        {
            if (myUserId != _playerId)
                return;

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((int)state);
            writer.Write((int)currentWeaponInput);
            writer.Write(isParalized);

            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);

            SendInput(stream, false);
        }
        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            if (showDebugInfo) Debug.Log($"{_playerId}--{_playerName}: Receiving movement inputs FROM client: {input}");
            
            state = (PlayerState)reader.ReadInt32();         
            currentWeaponInput = (PlayerShooterInputs)reader.ReadInt32();
            isParalized = reader.ReadBoolean();
            
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            directionMovement = Vector2.zero;
            //store new direction
            if (input[0] && !isParalized) directionMovement += Vector2.up;
            if (input[1] && !isParalized) directionMovement += Vector2.right;
            if (input[2] && !isParalized) directionMovement += Vector2.down;
            if (input[3] && !isParalized) directionMovement += Vector2.left;

            if (currentWeaponInput == PlayerShooterInputs.SHOOT)
            {
                currentWeaponInput = PlayerShooterInputs.SHOOT;
                _currentWeapon.ShootServer();
                OnShoot?.Invoke(_currentWeapon.localData);
                SendInputToClients();
            }
            else if (currentWeaponInput == PlayerShooterInputs.RELOAD)
            {
                _currentWeapon.Reload();
                OnReload?.Invoke(_currentWeapon.localData);
                SendInputToClients();
            }
        }

        public override void SendInputToClients()
        {
            if (showDebugInfo) Debug.Log($"{_playerId}--{_playerName}: Sending movement inputs TO clients: {input}");
            // Redirect input to other clients
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
 
            writer.Write((int)state);
            writer.Write((int)currentWeaponInput);
            writer.Write(isParalized);

            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);

            SendInput(stream, false);
        }

        public override void ReceiveInputFromServer(InputHeader header, BinaryReader reader)
        {
            if (isOwner())
            {
                int remainingBytes = (sizeof(Int32) * 2 + sizeof(bool) * 4);
                reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                return;
            }
            
            if (showDebugInfo) Debug.Log($"{_playerId}--{_playerName}: Receiving movement inputs FROM server: {input}");
            
            state = (PlayerState)reader.ReadInt32();
            currentWeaponInput = (PlayerShooterInputs)reader.ReadInt32();
            isParalized = reader.ReadBoolean();
            
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            directionMovement = Vector2.zero;
            
            if (input[0] && !isParalized) directionMovement += Vector2.up;
            if (input[1] && !isParalized) directionMovement += Vector2.right;
            if (input[2] && !isParalized) directionMovement += Vector2.down;
            if (input[3] && !isParalized) directionMovement += Vector2.left;
            
            if (currentWeaponInput == PlayerShooterInputs.SHOOT)
            {
                currentWeaponInput = PlayerShooterInputs.SHOOT;
                _currentWeapon.ShootClient();
                OnShoot?.Invoke(_currentWeapon.localData);
            }
            else if (currentWeaponInput == PlayerShooterInputs.RELOAD)
            {
                _currentWeapon.Reload();
                OnReload?.Invoke(_currentWeapon.localData);
            }
        }
    }
}