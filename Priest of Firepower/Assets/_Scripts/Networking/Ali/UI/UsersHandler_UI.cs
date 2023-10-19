using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

namespace ServerAli
{
    public class UsersHandler_UI : MonoBehaviour
    {
        public GameObject userPrefab;

        public Server_TCP serverTCP;
        private void Awake()
        {
            if (serverTCP == null)
                serverTCP = FindObjectOfType<Server_TCP>();

            if (serverTCP == null)
                return;
        }

        private void OnEnable()
        {
            //StartCoroutine(CreateUI());
            DestroyAllChildren();
            Compose();
        }

        //IEnumerator CreateUI()
        //{
        //    yield return StartCoroutine(DestroyAllChildren());
        //    yield return StartCoroutine(Compose());
        //}

        void Compose()
        {
            if (serverTCP == null)
                return;

            float height = userPrefab.GetComponent<RectTransform>().rect.height;
            float heighOffset = 0.0f;

            List<Socket> users = serverTCP.GetAciveClients();
            foreach (Socket user in users)
            {
                GameObject go = Instantiate(userPrefab);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.rect.position.Set(transform.position.x, transform.position.y + heighOffset);

                TextMeshProUGUI textMeshProUGUI = go.GetComponent<TextMeshProUGUI>();
                textMeshProUGUI.text = user.RemoteEndPoint.ToString();

                heighOffset += height;
            }
        }

        void DestroyAllChildren()
        {
            int num = transform.childCount;

            for (int i = num - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

        }
    }
}