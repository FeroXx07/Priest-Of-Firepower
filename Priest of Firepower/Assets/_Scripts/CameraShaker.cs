using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts;
using Cinemachine;
using JetBrains.Annotations;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField]private CinemachineVirtualCamera cmvCamera;
    private float shakeTimer, startingIntensity,totalIntensity;
    private static CameraShaker instance;
    public static CameraShaker Instance
    {
        get
        {
            if (instance == null)
            {
                // If the instance doesn't exist, create it
                instance = new GameObject("CameraShaker").AddComponent<CameraShaker>();
            }
            return instance;
        }
    }
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    public void Shake(float duration, float intensity)
    {
        CinemachineBasicMultiChannelPerlin noise = cmvCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = intensity;
        startingIntensity = intensity;
        totalIntensity = intensity;
        shakeTimer = duration;
    }
    private void Update()
    {
        if (cmvCamera)
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                CinemachineBasicMultiChannelPerlin noise = cmvCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                noise.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (startingIntensity / totalIntensity));
            }
            else
            {
                CinemachineBasicMultiChannelPerlin noise = cmvCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                noise.m_AmplitudeGain = 0f;
            }
        }
    }
}
