using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Networking
{
    public class Lobby : NetworkBehaviour
    {
        [SerializeField] GameObject clientUiPrefab;
        [SerializeField] Button startGameBtn;


        private void Start()
        {
    
        }

        protected override void InitNetworkVariablesList()
        {
            throw new System.NotImplementedException();
        }

        protected override MemoryStream Write(MemoryStream outputMemoryStream, NetworkAction action)
        {
            throw new System.NotImplementedException();
        }

        public override void Read(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}
