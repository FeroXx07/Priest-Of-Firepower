using System;
using System.IO;
using _Scripts.Enemies;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts
{
    public class RoundSystem : NetworkBehaviour
    {
        [SerializeField] private int _currentRound = 0;
        [SerializeField] public float timeBetweenRounds = 5f;
        [SerializeField] private float _timer;
        [SerializeField] private bool _isCountDown =false;
        
        public Action<int> OnRoundBegin;
        public Action OnRoundEnd;

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(_currentRound);
            writer.Write(timeBetweenRounds);
            writer.Write(_timer);
            writer.Write(_isCountDown);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }
        
        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            _currentRound = reader.ReadInt32();
            timeBetweenRounds = reader.ReadSingle();
            _timer = reader.ReadSingle();
            _isCountDown = reader.ReadBoolean();
            return true;
        }

        public int GetCurrentRound() { return _currentRound; }
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }
        protected override void InitNetworkVariablesList()
        {
            
        }
        public void StartRound()
        {
            _currentRound++;
            _isCountDown = false;
            OnRoundBegin?.Invoke(_currentRound);
            Debug.LogWarning("Round System: New round started and count down finished");
        }
        public void RoundFinished(EnemyManager enemyManager)
        {
            if (enemyManager.GetEnemiesCountLeft() <= 0 && enemyManager.GetEnemiesAlive() <= 0 && !_isCountDown)
            {
                Debug.LogWarning("Round System: round finished");
                OnRoundEnd?.Invoke();
                StartCountDown();
            }
        }

        public override void Update()
        {
            base.Update();
            
            if (_isCountDown)
            {
                _timer -= Time.deltaTime;

                if (_timer <= 0)
                {
                    StartRound();
                }
            }
        }

        private void StartCountDown()
        {
            _isCountDown = true;
            _timer = timeBetweenRounds;
            Debug.Log("Round System: Count down started");
        }
    }
}