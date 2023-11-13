using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using _Scripts.Networking;
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
            networkVariableList.Add(myNetVariableInt);
            networkVariableList.Add(myNetVariableStr);
            networkVariableList.Add(myNetFloat);
            networkVariableList.Add(myNetDouble);
        }

        protected override MemoryStream Write(MemoryStream outputMemoryStream)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            //Type objectType = this.GetType();
            //writer.Write(objectType.AssemblyQualifiedName);
            //writer.Write(networkObject.GetNetworkId());
            BitArray bitfield = bitTracker.GetBitfield();
            int fieldCount = bitfield.Length;
            writer.Write(fieldCount);
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);
            myNetVariableInt.WriteInBinaryWriter(writer);
            myNetVariableStr.WriteInBinaryWriter(writer);
            myNetFloat.WriteInBinaryWriter(writer);
            myNetDouble.WriteInBinaryWriter(writer);
            return outputMemoryStream;
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            bitTracker = new ChangeTracker(networkVariableList.Count);
            networkVariableList.ForEach(var => var.SetTracker(bitTracker));
        }
        private void Start()
        {
            bitTracker.SetAll(true);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            MemoryStream testStream = new MemoryStream();
            Write(testStream);

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
            stopwatch.Stop();
            long rTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Read Elapsed Time: {rTime} milliseconds");
        }

        private void Update()
        {
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariableInt is: {myNetVariableInt.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariableStr is: {myNetVariableStr.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariablefloat is: {myNetDouble.Value}");
            UnityEngine.Debug.Log($"{gameObject.name} myNetVariabledouble is: {myNetFloat.Value}");
        }
    }
}
