using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts
{
    public class PointSystem : NetworkBehaviour
    {
        [SerializeField] private int _points;
        [SerializeField] public int multiplier = 1;
        public Action<int> OnPointsAdded;
        public Action<int> OnPointsRemoved;
        public Action<int> OnPointsChanged;
        [SerializeField] private Player.Player player;
        
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        void Start()
        {
            _points = 0;
            //Show points on start
            OnPointsChanged?.Invoke(_points);
            player = GetComponent<Player.Player>();
        }

        private void AddPoints(int pointsToAdd)
        {
            Debug.Log($"Point System: Adding points. Name: {player.name}. Owner: {player.isOwner()}. Points: {pointsToAdd}");
            _points += pointsToAdd;
            OnPointsAdded?.Invoke(pointsToAdd);
            OnPointsChanged?.Invoke(_points);
        }

        public void RemovePoints(int pointsToRemove)
        {
            Debug.Log($"Point System: Removing points. Name: {player.name}. Owner: {player.isOwner()}. Points: {pointsToRemove}");
            _points -= pointsToRemove;
            OnPointsRemoved?.Invoke(pointsToRemove);
            OnPointsChanged?.Invoke(_points);
        }

        public int GetPoints()
        {
            return _points;
        }

        public void PointsOnHit(IPointsProvider pointsProvider)
        {
            int points = pointsProvider.ProvidePointsOnHit() * multiplier;
            AddPoints(points);
        }

        public void PointsOnDeath(IPointsProvider pointsProvider)
        {
            int points = pointsProvider.ProvidePointsOnDeath() * multiplier;
            AddPoints(points);
        }

        protected override void InitNetworkVariablesList()
        {
            
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(_points);
            writer.Write(multiplier);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            _points = reader.ReadInt32();
            multiplier = reader.ReadInt32();
            OnPointsChanged?.Invoke(_points);
            return true;
        }
    }
}