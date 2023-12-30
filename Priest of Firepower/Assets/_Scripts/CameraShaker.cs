using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts;
using Cinemachine;
using JetBrains.Annotations;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }
    
    [SerializeField]private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    [SerializeField] private float _duration;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("Virtual Camera not assigned in the inspector.");
            return;
        }

        noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    // Update is called once per frame
    void Update()
    {
        // if (_duration > 0)
        // {
        //     _duration -= Time.deltaTime;
        // }
        // else
        // {
        //     noise.m_AmplitudeGain = 0;
        // }
    }

    public void Shake(float intesity, float duration)
    {
        noise.m_AmplitudeGain = intesity;
        _duration = duration;
    }
}
