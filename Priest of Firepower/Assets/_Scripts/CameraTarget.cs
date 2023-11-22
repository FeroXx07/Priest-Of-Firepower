using System;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts
{
    public class CameraTarget : MonoBehaviour
    {
        [SerializeField] private Vector3 playerPos = Vector3.zero;
        [SerializeField] private Camera mainCamera;
        [Range(2, 100)][SerializeField] private float cameraTargetDivider;
        
        private void SetPlayer(GameObject obj)
        {
            playerPos = NetworkManager.Instance.player.transform.position;
        }
        private void OnEnable()
        {
            NetworkManager.Instance.OnHostPlayerCreated += SetPlayer;
        }
        private void OnDisable()
        {
            NetworkManager.Instance.OnHostPlayerCreated -= SetPlayer;
        }

        private void Update()
        {
            var mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var cameraTargetPosition = (mousePosition + (cameraTargetDivider - 1) * playerPos) / cameraTargetDivider;
            transform.position = cameraTargetPosition;
        }
    }
}