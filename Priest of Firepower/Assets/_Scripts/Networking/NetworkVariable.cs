using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Networking
{
    public interface INetworkVariable
    {
        object GetValue();
        void SetValue(object newValue, bool isFromNetwork = false);
        void WriteInBinaryWriter(BinaryWriter writer);
        void ReadFromBinaryReader(BinaryReader reader);
        bool IsDirty { get; }
        bool IsLastValueFromNetwork { get; }
        void SetTracker(ChangeTracker changeTracker);
    }

    [Serializable]
    public class NetworkVariable<T> : INetworkVariable
    {
        public NetworkVariable(T value, int trackerIndex)
        {
            this.value = value;
            _type = value.GetType();
            _trackerIndex = trackerIndex;

            if (_changeTracker != null)
                _changeTracker.TrackChange(_trackerIndex);
            isDirty = false;
            isLastValueFromNetwork = false;
        }

        [SerializeField] private T value;
        [SerializeField] Type _type;
        [SerializeField] private bool isDirty = false;
        [SerializeField] private bool isLastValueFromNetwork = false;
        private ChangeTracker _changeTracker = null;
        private int _trackerIndex;
        public T Value { get => value; set => SetValue(value); }
        public bool IsDirty => isDirty;
        public bool IsLastValueFromNetwork => isLastValueFromNetwork;
        public void SetTracker(ChangeTracker changeTracker)
        {
            _changeTracker = changeTracker;
        }

        public void SetValue(object newValue, bool isFromNetwork = false)
        {
            if (value.Equals(newValue)) return;
            
            isDirty = true;
            isLastValueFromNetwork = isFromNetwork;

            if (_changeTracker != null && isLastValueFromNetwork == false)
                _changeTracker.TrackChange(_trackerIndex);
                
            value = (T)newValue;
        }

        public void WriteInBinaryWriter(BinaryWriter writer) // Supported formats are
        {
            if (writer == null)
            {
                Debug.Log("Writer is null");
                return;
            }
            
            if (_changeTracker != null)
                _changeTracker.DeTrackChange(_trackerIndex);
            
            isDirty = false;
            isLastValueFromNetwork = false;
            
            if (_type == typeof(int))
            {
                int castedValue = Convert.ToInt32(value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(bool))
            {
                bool castedValue = Convert.ToBoolean(value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(uint))
            {
                uint castedValue = Convert.ToUInt32(value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(ulong))
            {
                ulong castedValue = Convert.ToUInt64(value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(long))
            {
                long castedValue = Convert.ToInt64(value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(string))
            {
                string castedValue = value.ToString();
                writer.Write(castedValue);
            }
            else if (_type == typeof(float))
            {
                float castedValue = (float)Convert.ChangeType(value, typeof(float));
                writer.Write(castedValue);
            }
            else if (_type == typeof(double))
            {
                double castedValue = Convert.ToDouble(value);
                writer.Write(castedValue);
            }
        }

        public void ReadFromBinaryReader(BinaryReader reader) // Supported formats are
        {
            if (reader == null)
            {
                Debug.Log("Reader is null");
                return;
            }

            if (_changeTracker != null)
                _changeTracker.DeTrackChange(_trackerIndex);
            
            isDirty = false;
            isLastValueFromNetwork = true;

            if (_type == typeof(int))
                value = (T)(object)reader.ReadInt32();
            else if (_type == typeof(bool))
                value = (T)(object)reader.ReadBoolean();
            else if (_type == typeof(string))
                value = (T)(object)reader.ReadString();
            else if (_type == typeof(float))
                value = (T)(object)reader.ReadSingle();
            else if (_type == typeof(double))
                value = (T)(object)reader.ReadDouble();
            else if (_type == typeof(uint))
                value = (T)(object)reader.ReadUInt32();
            else if (_type == typeof(ulong))
                value = (T)(object)reader.ReadUInt64();
            else if (_type == typeof(long))
                value = (T)(object)reader.ReadInt64();
            else
                throw new NotSupportedException($"Deserialization of type {_type} is not supported.");
        }

        public object GetValue()
        {
            return Value;
        }
    }
}