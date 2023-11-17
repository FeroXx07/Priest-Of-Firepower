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
          
        }

        protected override bool Write(MemoryStream outputMemoryStream, NetworkAction action)
        {
            return false;
        }

        public override bool Read(BinaryReader reader, long currentPosition = 0)
        {
            return false;
        }
    }
}
