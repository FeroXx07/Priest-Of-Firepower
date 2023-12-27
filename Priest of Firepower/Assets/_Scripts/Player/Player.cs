using _Scripts.Networking;
using System;
using System.Collections;
using System.IO;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
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
            
            GetComponent<NetworkObject>().speed = speed;
            
            clientSendReplicationData = true;
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
            shootMarker.positionCount = 2;
            _weaponFlipped = false;
            NetworkManager.Instance.AnyPlayerCreated(gameObject);

            if (isOwner())
            {
                Debug.Log("Initiating host player and UI");
                GetComponent<SpriteRenderer>().sortingOrder = 11;
                FindObjectOfType<CinemachineVirtualCamera>().Follow = transform;
                NetworkManager.Instance.player = gameObject;
                NetworkManager.Instance.OwnerPlayerCreated(gameObject);
                GameManager.Instance.StartGame();
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
            directionMovement = Vector2.zero;
            currentWeaponInput = PlayerShooterInputs.NONE;
            
            if (Input.GetMouseButtonDown(0))
            {
                currentWeaponInput = PlayerShooterInputs.SHOOT;
                OnShoot?.Invoke();
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
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentWeaponInput = PlayerShooterInputs.RELOAD;
                _currentWeapon.Reload();
                OnReload?.Invoke();
                if (isHost)
                {
                    SendInputToClients();
                }
                else if (isClient)
                {
                    SendInputToServer();
                }
            }
            
            if (Input.GetKey(KeyCode.W) && !isParalized)
            {
                input[0] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.up;
            }
            else
            {
                input[0] = false;
            }

            if (Input.GetKey(KeyCode.D) && !isParalized)
            {
                input[1] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.right;
            }
            else
            {
                input[1] = false;
            }

            if (Input.GetKey(KeyCode.S) && !isParalized)
            {
                input[2] = true;
                hasChangedMovement = true;
                directionMovement += Vector2.down;
            }
            else
            {
                input[2] = false;
            }

            if (Input.GetKey(KeyCode.A) && !isParalized)
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
                    NetworkObject.WriteReplicationTransform(TransformAction.INTERPOLATE);
                }
                state = PlayerState.IDLE;
            }
            
            if (hasChangedMovement)
            {
                hasChangedMovement = false;
                if (NetworkManager.Instance.IsClient())
                {    
                    if (showDebugInfo) Debug.Log("sending state" + state);
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

            if (_weaponHolder != null)
            {
                var transform1 = _weaponHolder.transform;
                transform1.rotation = targetRotation;
                transform1.position = transform.position + shootDir * weaponOffset;  
            }
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
  
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);

            SendInput(stream, false);
        }
        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            if (showDebugInfo) Debug.Log($"{_playerId}--{_playerName}: Receiving movement inputs FROM client: {input}");


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
                _currentWeapon.ShootServer();
                OnShoot?.Invoke();
                SendInputToClients();
            }
            else if (currentWeaponInput == PlayerShooterInputs.RELOAD)
            {
                _currentWeapon.Reload();
                OnReload?.Invoke();
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
            else if (currentWeaponInput == PlayerShooterInputs.RELOAD)
            {
                _currentWeapon.Reload();
                OnReload?.Invoke();
            }
        }
    }
}