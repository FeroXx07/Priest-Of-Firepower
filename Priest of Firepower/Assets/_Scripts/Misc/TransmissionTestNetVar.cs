using _Scripts.Networking;
using UnityEngine;

namespace _Scripts.Misc
{
    public class TransmissionTestNetVar : NetworkBehaviour
    {
        [SerializeField] private NetworkVariable<int> variableA = new NetworkVariable<int>(100, 0); 
        [SerializeField] private NetworkVariable<string> variableB = new NetworkVariable<string>("Hello", 1); 
        protected override void InitNetworkVariablesList()
        {
            NetworkVariableList.Add(variableA);
            NetworkVariableList.Add(variableB);
        }
        
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            NetworkVariableList.ForEach(var => var.SetTracker(BITTracker));
        }
    }
}
