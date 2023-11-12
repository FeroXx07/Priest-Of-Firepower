using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NetworkVariable<T>
{
    public NetworkVariable(T value, int trackerIndex, ChangeTracker changeTracker)
    {
        _value = value;
        _trackerIndex = trackerIndex;
        _changeTracker = changeTracker;
    }

    private T _value;
    private bool isDirty = false;
    private ChangeTracker _changeTracker;
    private int _trackerIndex;
    public T Value { get => _value; set => SetValue(value); }
    public bool IsDirty { get => isDirty; }

    void SetValue(T newValue) 
    { 
        if (_value.Equals(newValue) == false)
        {
            isDirty = true;
            _changeTracker.TrackChange(_trackerIndex);
        }
        _value = newValue;
    }
}
