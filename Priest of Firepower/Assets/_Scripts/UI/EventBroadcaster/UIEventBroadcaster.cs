using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Scripts.UI.EventBroadcasting
{
    public class UIEventBroadcaster : MonoBehaviour
    {
        [SerializeField] GameObject eventTilePrefab;
        [SerializeField] List<GameObject> tileList = new List<GameObject>();

        [SerializeField] Color tempCol;
        //43 chars per line
        //height = 30 for single line


        public enum PlaygroundEvent
        {
            PLAYER_DEATH = 1,
            PLAYER_REVIVE = 2,
            WAVE_START = 3,
            WAVE_FINISH = 4,
            POWERUP_PICKUP = 5,
            OPEN_DOOR = 6,
            OPEN_CHEST = 7,
            BOSS_SPAWN = 8,
            BOSS_DEATH = 9,

        }

        private KeyCode[] keyCodes = {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
        };

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    CreateTile((PlaygroundEvent)(i + 1));
                }

            }
        }

        void CreateTile(PlaygroundEvent ev)
        {
            GameObject tile = Instantiate(eventTilePrefab);

            tileList.Add(tile);

            Image bg = tile.transform.GetChild(0).GetComponent<Image>();
            TextMeshProUGUI text = tile.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

            string strBgCol = "#FFFFFF";
            string strTextCol = "#FFFFFF";

            switch (ev)
            {
                case PlaygroundEvent.PLAYER_DEATH:
                    {
                        strBgCol = "#FF0000";
                        strTextCol = "#FFFFFF";
                    }
                    break;
                case PlaygroundEvent.PLAYER_REVIVE:
                    {
                        strBgCol = "#00FF3A";
                        strTextCol = "#00FFBF";
                    }
                    break;
                case PlaygroundEvent.WAVE_START:
                    {
                        strBgCol = "#00BFFF";
                        strTextCol = "#BDEFFF";

                    }
                    break;
                case PlaygroundEvent.WAVE_FINISH:
                    {
                        strBgCol = "#0200FF";
                        strTextCol = "#F4E9FF";
                    }
                    break;
                case PlaygroundEvent.POWERUP_PICKUP:
                    {
                        //switch per each pickup?

                        strBgCol = "#FFF700";
                        strTextCol = "#FFF2DF";
                    }
                    break;
                case PlaygroundEvent.OPEN_DOOR:
                    {
                        strBgCol = "#C87527";
                        strTextCol = "#FFFFFF";
                    }
                    break;
                case PlaygroundEvent.OPEN_CHEST:
                    {
                        strBgCol = "#FFAE00";
                        strTextCol = "#FFE5BD";
                    }
                    break;
                case PlaygroundEvent.BOSS_SPAWN:
                    {
                        strBgCol = "#9C3EFF";
                        strTextCol = "#DDBDFF";
                    }
                    break;
                case PlaygroundEvent.BOSS_DEATH:
                    {
                        strBgCol = "#FF3DD1";
                        strTextCol = "#FFBDFB";
                    }
                    break;
                default:

                    break;
            }

            Color bgCol;
            Color textCol;

            ColorUtility.TryParseHtmlString(strBgCol, out bgCol);
            ColorUtility.TryParseHtmlString(strTextCol, out textCol);

            bgCol.a = 0.25f;
            textCol.a = 1;

            bg.color = bgCol;
            text.color = textCol;


            //AdjustHeight(ref tile, text, 43);

            //Rect r = new Rect(tile.GetComponent<RectTransform>().rect);
            //r.height = 60;
            //tile.GetComponent<RectTransform>().rect.Set(r.x, r.y, r.width, r.height);
            //tile.GetComponent<RectTransform>().ForceUpdateRectTransforms();
            //print(r);

            //tile.transform.parent = transform;

        }

        void AdjustHeight(ref GameObject tile, TextMeshProUGUI text, int maxTextLenght)
        {

            int tileHeight = 30;

            while (text.text.Length > maxTextLenght)
            {
                maxTextLenght *= 2;
                tileHeight += 30;
            }

            print(text.text.Length);
            print(tileHeight);

            Rect r = new Rect(tile.GetComponent<RectTransform>().rect);
            tile.GetComponent<RectTransform>().rect.Set(r.x, r.y, r.width, tileHeight);

            
        }
    }
}
