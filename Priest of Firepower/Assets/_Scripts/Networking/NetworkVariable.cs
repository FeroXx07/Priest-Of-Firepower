using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NetworkVariable<T>
{
    public NetworkVariable(T value, int trackerIndex)
    {
        _type = typeof(T);
        _value = value;
        _trackerIndex = trackerIndex;
    }

    private T _value;
    private Type _type;
    private bool isDirty = false;
    private ChangeTracker _changeTracker;
    private int _trackerIndex;
    public T Value { get => _value; set => SetValue(value); }
    public bool IsDirty { get => isDirty; }
    public void SetTracker(ChangeTracker changeTracker)
    {
        _changeTracker = changeTracker;
    }

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
