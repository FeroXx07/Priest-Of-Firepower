using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Networking
{
    public interface INetworkVariable
    {
        object GetValue();
        void SetValue(object newValue);
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
            _value = value;
            _type = value.GetType();
            _trackerIndex = trackerIndex;

            if (_changeTracker != null)
                _changeTracker.TrackChange(_trackerIndex);
            _isDirty = true;
        }

        [SerializeField] private T _value;
        [SerializeField] Type _type;
        [SerializeField] private bool _isDirty = false;
        [SerializeField] private bool _isLastValueFromNetwork = false;

        private ChangeTracker _changeTracker = null;
        private int _trackerIndex;
        public T Value { get => _value; set => SetValue(value); }
        public bool IsDirty => _isDirty;
        public bool IsLastValueFromNetwork => _isLastValueFromNetwork;
        public void SetTracker(ChangeTracker changeTracker)
        {
            _changeTracker = changeTracker;
        }

        public void SetValue(object newValue) 
        { 
            if (_value.Equals(newValue) == false)
            {
                _isDirty = true;
                _isLastValueFromNetwork = false;

                if (_changeTracker != null)
                    _changeTracker.TrackChange(_trackerIndex);
            }
            _value = (T)newValue;
        }

        public void WriteInBinaryWriter(BinaryWriter writer) // Supported formats are
        {
            if (writer == null)
            {
                Debug.Log("Writer is null");
                return;
            }

            if (_type == typeof(int))
            {
                int castedValue = Convert.ToInt32(_value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(bool))
            {
                bool castedValue = Convert.ToBoolean(_value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(uint))
            {
                uint castedValue = Convert.ToUInt32(_value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(ulong))
            {
                ulong castedValue = Convert.ToUInt64(_value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(long))
            {
                long castedValue = Convert.ToInt64(_value);
                writer.Write(castedValue);
            }
            else if (_type == typeof(string))
            {
                string castedValue = _value.ToString();
                writer.Write(castedValue);
            }
            else if (_type == typeof(float))
            {
                float castedValue = (float)Convert.ChangeType(_value, typeof(float));
                writer.Write(castedValue);
            }
            else if (_type == typeof(double))
            {
                double castedValue = Convert.ToDouble(_value);
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
            _isDirty = false;
            _isLastValueFromNetwork = true;

            if (_type == typeof(int))
                _value = (T)(object)reader.ReadInt32();
            else if (_type == typeof(bool))
                _value = (T)(object)reader.ReadBoolean();
            else if (_type == typeof(string))
                _value = (T)(object)reader.ReadString();
            else if (_type == typeof(float))
                _value = (T)(object)reader.ReadSingle();
            else if (_type == typeof(double))
                _value = (T)(object)reader.ReadDouble();
            else if (_type == typeof(uint))
                _value = (T)(object)reader.ReadUInt32();
            else if (_type == typeof(ulong))
                _value = (T)(object)reader.ReadUInt64();
            else if (_type == typeof(long))
                _value = (T)(object)reader.ReadInt64();
            else
                throw new NotSupportedException($"Deserialization of type {_type} is not supported.");
        }

        public object GetValue()
        {
            return Value;
        }
    }
}