using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts.Spawners
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private EnemySpawner[] spawners;
        public List<Player.Player> playersList = new List<Player.Player>();
        public bool isActive = false;
        private void Awake()
        {
            if (!NetworkManager.Instance.IsHost()) return;
            spawners = GetComponentsInChildren<EnemySpawner>();
        }

        private void Update()
        {
            if (playersList.Count > 0 && !isActive)
            {
                foreach (EnemySpawner enemySpawner in spawners)
                    enemySpawner.Activate();

                isActive = true;
            }
            else if (playersList.Count == 0)
            {
                foreach (EnemySpawner enemySpawner in spawners)
                    enemySpawner.DeActivate();

                isActive = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!NetworkManager.Instance.IsHost()) return;
            if (!other.TryGetComponent<Player.Player>(out Player.Player player)) return;
            if (!playersList.Contains(player)) playersList.Add(player);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!NetworkManager.Instance.IsHost()) return;
            if (!other.TryGetComponent<Player.Player>(out Player.Player player)) return;
            if (!playersList.Contains(player)) playersList.Add(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!NetworkManager.Instance.IsHost()) return;
            if (!other.TryGetComponent<Player.Player>(out Player.Player player)) return;
            if (playersList.Contains(player)) playersList.Remove(player);
        }
    }
}
