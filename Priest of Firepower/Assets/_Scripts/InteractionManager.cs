using System;
using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts
{
    [RequireComponent(typeof(NetworkObject))]
    public class InteractionManager : NetworkBehaviour
    {
        public class Interaction
        {
            // network obj ids
            private UInt64 interactorId; 
            private UInt64 interactableId;
            private UInt64 startInteractionTime;
        
        }
    
        #region Singleton
        private static InteractionManager _instance;
        public static InteractionManager Instance
        {
            get
            {
                // if instance is null
                if (_instance == null)
                {
                    // find the generic instance
                    _instance = FindObjectOfType<InteractionManager>();

                    // if it's null again create a new object
                    // and attach the generic instance
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(InteractionManager).Name;
                        _instance = obj.AddComponent<InteractionManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion
    
    
        void Start()
        {
        
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        public override void Update()
        {
            base.Update();
        }

        public void ServerRecieveInteraction()
        {
        
        }

        public void ServerSendInteraction()
        {
        
        }

        public void ClientSendInteraction(Interactor interactor,NetworkBehaviour interactableNbh)
        {
            MemoryStream managerStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(managerStream);

            NetworkBehaviour interactiorNbh = interactor.GetComponent<NetworkBehaviour>();

        
            // writer.Write(interactionData.ToArray());        
        
            ReplicationHeader managerHeader =
                new ReplicationHeader(GetObjId(), GetType().FullName, ReplicationAction.UPDATE,managerStream.ToArray().Length);
       
            NetworkManager.Instance.AddStateStreamQueue(managerHeader,managerStream);
        }

        public void ClientReceiveInteraction()
        {
        
        }
    
        protected override void InitNetworkVariablesList()
        {
     
        }
    }
}
