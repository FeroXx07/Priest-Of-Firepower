using Cinemachine;
using UnityEngine;

namespace _Scripts
{
    public class CameraShaker : GenericSingleton<CameraShaker>
    {
        [SerializeField] private CinemachineVirtualCamera cmvCamera;
        private float shakeTimer, startingIntensity,totalIntensity;
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
}
