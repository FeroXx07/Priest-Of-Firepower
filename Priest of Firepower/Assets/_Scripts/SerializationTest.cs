using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using _Scripts.Networking;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts
{
    [Serializable]
    public class SerializationTest : NetworkBehaviour
    {
        [SerializeField] public NetworkVariable<int> myNetVariableInt = new NetworkVariable<int>(10101, 0);
        [SerializeField] public NetworkVariable<string> myNetVariableStr = new NetworkVariable<string>("My Name is Ali", 1);
        [SerializeField] public NetworkVariable<float> myNetFloat = new NetworkVariable<float>(333.333f, 2);
        [SerializeField] public NetworkVariable<double> myNetDouble = new NetworkVariable<double>(99.999993, 3);

        protected override void InitNetworkVariablesList()
        {
            // Add your NetworkVariable instances to the list
            NetworkVariableList.Add(myNetVariableInt);
            NetworkVariableList.Add(myNetVariableStr);
            NetworkVariableList.Add(myNetFloat);
            NetworkVariableList.Add(myNetDouble);
        }

        protected override MemoryStream Write(MemoryStream outputMemoryStream, NetworkAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            // Type objectType = this.GetType();
            // writer.Write(objectType.AssemblyQualifiedName);
            // writer.Write(NetworkObject.GetNetworkId());
            // writer.Write((int)action);
            
            BitArray bitfield = BITTracker.GetBitfield();
            int fieldCount = bitfield.Length;
            writer.Write(fieldCount);
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);
            
            myNetVariableInt.WriteInBinaryWriter(writer);
            myNetVariableStr.WriteInBinaryWriter(writer);
            myNetFloat.WriteInBinaryWriter(writer);
            myNetDouble.WriteInBinaryWriter(writer);
            
            BITTracker.SetAll(false);
            return outputMemoryStream;
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            NetworkVariableList.ForEach(var => var.SetTracker(BITTracker));
        }
        private void Start()
        {
            BITTracker.SetAll(true);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            MemoryStream testStream = new MemoryStream();
            Write(testStream, NetworkAction.UPDATE);
            MemoryStream transformStream = NetworkObject.SendNetworkTransform();
            stopwatch.Stop();
            long wTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Write Elapsed Time: {wTime} milliseconds");

            int timer = 0;

            while(timer < 100000)
            {
                timer++;
            }

            myNetVariableStr.Value = "HOLAAAAAAAAAAAAAAA";
            myNetVariableInt.Value = 20;
            myNetFloat.Value = 222.22f;
            myNetDouble.Value = 278.2738946;
            transform.SetPositionAndRotation(new Vector3(10,10,0), quaternion.identity);
            //b = false;
            //ui = 1;
            //i = -1;
            //f = 99.3f;
            //d = 200.4444;
            //s = "goodbye";
            //ul = 300040004000;
            BinaryReader binaryReader = new BinaryReader(testStream);
            stopwatch.Restart();
            Read(binaryReader);
            
            BinaryReader trReader = new BinaryReader(transformStream);
            NetworkObject.HandleNetworkTransform(trReader);
            stopwatch.Stop();
            long rTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Read Elapsed Time: {rTime} milliseconds");
        }

        public override void Update()
        {
            base.Update();
            
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariableInt is: {myNetVariableInt.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariableStr is: {myNetVariableStr.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariablefloat is: {myNetDouble.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariabledouble is: {myNetFloat.Value}");
        }
    }
}
