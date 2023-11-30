using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Player
{
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
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private bool isHost => NetworkManager.Instance.IsHost();
        [SerializeField] private UInt64 myId => NetworkManager.Instance.getId;
        [SerializeField] private Player player;
        bool hasChanged = false;
        public float tickRatePlayer = 10.0f; // Network writes inside a second.
        [SerializeField] private float tickCounter = 0.0f;
        private Vector2 direction;
        [HideInInspector] public PlayerState state;
        protected override void InitNetworkVariablesList()
        {
            //throw new NotImplementedException();
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
            player = GetComponent<Player>();
            _rb = GetComponent<Rigidbody2D>();
            state = PlayerState.IDLE;
        }
        public float speed;
        private bool[] input = new bool[4];
        public Rigidbody2D _rb;
        public override void Update()
        {
            base.Update();
            // Only the owner of the player will control it
            if (!player.isOwner()) return;
            
            direction = Vector2.zero;

            // input[0] = Input.GetKey(KeyCode.W);
            // input[1] = Input.GetKey(KeyCode.D);
            // input[2] = Input.GetKey(KeyCode.S);
            // input[3] = Input.GetKey(KeyCode.A);
            
            
            if (Input.GetKey(KeyCode.W))
            {
                input[0] = true;
                hasChanged = true;
                direction += Vector2.up;
            }
            else
            {
                input[0] = false;
            }

            if (Input.GetKey(KeyCode.D))
            {
                input[1] = true;
                hasChanged = true;
                direction += Vector2.right;
            }
            else
            {
                input[1] = false;
            }

            if (Input.GetKey(KeyCode.S))
            {
                input[2] = true;
                hasChanged = true;
                direction += Vector2.down;
            }
            else
            {
                input[2] = false;
            }

            if (Input.GetKey(KeyCode.A))
            {
                input[3] = true;
                hasChanged = true;
                direction += Vector2.left;
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
            
            if (hasChanged)
            {
                hasChanged = false;
                if (NetworkManager.Instance.IsClient())
                {   
                    float finalRate = 1.0f / tickRatePlayer;
                    if (tickCounter >= finalRate)
                    {
                        tickCounter = 0.0f;
                        SendInputToServer();
                    }
                    tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;
                    tickCounter += Time.deltaTime;
                }
                else if (NetworkManager.Instance.IsHost())
                {
                    //SendInputToClients();
 
                }
            }
        }
        public void FixedUpdate()
        {
            //apply velocity to the client 
           _rb.velocity = direction.normalized * speed;
        }
        public string nameIdentifier => "PlayerMovement";
        public override void SendInputToServer()
        {
            if (myId != player.GetPlayerId())
                return;
            
            //Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Sending movement inputs TO server: {input}");
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write((int)state);
            if(state == PlayerState.IDLE)
                Debug.Log(state);
            writer.Write(player.GetPlayerId());
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
            
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }
        public override void ReceiveInputFromClient(BinaryReader reader)
        {
            Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Receiving movement inputs FROM client: {input}");

            state = (PlayerState)reader.ReadInt32();
            
            UInt64 id = reader.ReadUInt64();
            
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            direction = Vector2.zero;
            //store new direction
            if (input[0]) direction += Vector2.up;
            if (input[1]) direction += Vector2.right;
            if (input[2]) direction += Vector2.down;
            if (input[3]) direction += Vector2.left;
        }

        public override void SendInputToClients()
        {
            Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Sending movement inputs TO clients: {input}");
            // Redirect input to other clients
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write(player.GetPlayerId());
            
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
            
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }

        public override void ReceiveInputFromServer(BinaryReader reader)
        {
            Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Receiving movement inputs FROM server: {input}");

            state = (PlayerState)reader.ReadInt32();
            
            UInt64 id = reader.ReadUInt64();
            
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            
            direction = Vector2.zero;
            //store new direction
            if (input[0]) direction += Vector2.up;
            if (input[1]) direction += Vector2.right;
            if (input[2]) direction += Vector2.down;
            if (input[3]) direction += Vector2.left;
            // UInt64 id = reader.ReadUInt64();
            // if (id != player.GetPlayerId())
            // {
            //     int offset = (sizeof(bool)*4);
            //     reader.BaseStream.Seek(offset, SeekOrigin.Current); 
            //     return;
            // }
            // Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Receiving movement inputs FROM server: {input}");
            // for (int i = 0; i < 4; i++)
            //     input[i] = reader.ReadBoolean();
        }
    }
}
