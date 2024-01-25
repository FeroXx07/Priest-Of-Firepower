using _Scripts.Networking;
using UnityEngine;

namespace _Scripts.UI.TeamList
{
    public class UITeamListController : MonoBehaviour
    {
        public GameObject playerTilePrefab;
        public GameObject listRef;
        private void OnEnable()
        {
            NetworkManager.Instance.OnAnyPlayerCreated += Init;
        }
        private void OnDisable()
        {
            NetworkManager.Instance.OnAnyPlayerCreated += Init;
        }
        private void Init(GameObject playerObj)
        {
            UITeamListTile tile = Instantiate(playerTilePrefab, listRef.transform).GetComponent<UITeamListTile>();
            tile.Init(playerObj.GetComponent<Player.Player>());
        }
    }
}
