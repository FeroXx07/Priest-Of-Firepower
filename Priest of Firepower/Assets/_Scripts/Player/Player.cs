using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class Player : MonoBehaviour
    {
        public void SetPlayerId(UInt64 id) => _playerId = id;
        public UInt64 GetPlayerId() => _playerId;
        [SerializeField] private UInt64 _playerId;

        public void SetName(string name) => _playerName = name;
        public string GetName() => _playerName;
        [SerializeField] private string _playerName;
    }
}
