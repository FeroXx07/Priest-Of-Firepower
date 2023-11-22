using System;
using System.Collections.Generic;
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
    
    public class PlayerMovement : NetworkBehaviour, INetworkInput
    {
        [SerializeField] private bool isHost => NetworkManager.Instance.IsHost();
        public NetworkObject networkObject;
        protected override void InitNetworkVariablesList()
        {
            //throw new NotImplementedException();
        }

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
            input[0] = false; 
            input[1] = false; 
            input[2] = false; 
            input[3] = false; 
            _rb = GetComponent<Rigidbody2D>();
        }

        public float speed;

        private bool[] input = new bool[4];
        //public Dictionary<PlayerMovementInputs, bool> inputs = new Dictionary<PlayerMovementInputs, bool>();
        public Rigidbody2D _rb;
        
        void Update()
        {
            if (Input.GetKey(KeyCode.W)) input[0] = true;
            if (Input.GetKey(KeyCode.D)) input[1] = true;
            if (Input.GetKey(KeyCode.S)) input[2] = true;
            if (Input.GetKey(KeyCode.A)) input[3] = true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            SendInputToServer();

            if (isHost)
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

        public void SendInputToServer()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            for (int i = 0; i < 4; i++)
                writer.Write(input[i]);
        }

        public void ReceiveInputFromClient(BinaryReader reader)
        {
            for (int i = 0; i < 4; i++)
                input[i] = reader.ReadBoolean();
        }
    }
}
