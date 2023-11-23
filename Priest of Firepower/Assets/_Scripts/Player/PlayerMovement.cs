using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;
namespace _Scripts.Player
{
    public enum PlayerMovementInputs
    {
        UP = 0,
        RIGHT = 1,
        DOWN = 2,
        LEFT = 3
    }
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private bool isHost => NetworkManager.Instance.IsHost();
        [SerializeField] private UInt64 myId => NetworkManager.Instance.getId;
        [SerializeField] private Player player;
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
        }
        public float speed;
        private bool[] input = new bool[4];
        public Rigidbody2D _rb;
        public override void Update()
        {
            base.Update();
            // Only the owner of the player will control it
            if (myId == player.GetPlayerId())
            {
                if (Input.GetKey(KeyCode.W)) input[0] = true;
                if (Input.GetKey(KeyCode.D)) input[1] = true;
                if (Input.GetKey(KeyCode.S)) input[2] = true;
                if (Input.GetKey(KeyCode.A)) input[3] = true;
            }
        }
        public void FixedUpdate()
        {
            SendInputToServer();
            
            // Only the host machine will move all the players
            if (NetworkManager.Instance.IsHost())
            {
                Vector2 direction = Vector2.zero;
                if (input[0]) direction += Vector2.up;
                if (input[1]) direction += Vector2.right;
                if (input[2]) direction += Vector2.down;
                if (input[3]) direction += Vector2.left;
                _rb.velocity = direction * speed;
            }
            
            for (int i = 0; i < 4; i++)
                input[i] = false;
        }
        public string nameIdentifier => "PlayerMovement";
        public override void SendInputToServer()
        {
            // Send inputs of the player movement to the server, the host shouldn't send inputs to self.
            if (isHost || myId != player.GetPlayerId())
                return;
            
            //Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Sending movement inputs to server: {input}");
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }
        public override void ReceiveInputFromClient(BinaryReader reader)
        {
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
            //Debug.Log($"{player.GetPlayerId()}--{player.GetName()}: Receiving inputs from clients: {input}");
        }
    }
}
