using _Scripts.Networking;
using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class Player : MonoBehaviour
    {
        public void SetPlayerId(UInt64 id) => _playerId = id;
        public bool isOwner() => _playerId == NetworkManager.Instance.getId ? true:false;
        public UInt64 GetPlayerId() => _playerId;
        [SerializeField] private UInt64 _playerId;

        public void SetName(string name) => _playerName = name;
        public string GetName() => _playerName;
        [SerializeField] private string _playerName;


        private void Start()
        {
            //if host render on top the player over the others
            if(isOwner())
                GetComponent<SpriteRenderer>().sortingOrder = 11;
        }
    }
}
