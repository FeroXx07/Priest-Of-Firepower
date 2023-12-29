using UnityEngine;

namespace _Scripts
{
    public class CursorManager : GenericSingleton<CursorManager>
    {
        [SerializeField] private Texture2D cursorTexture;
        void Start()
        {
            Cursor.SetCursor(cursorTexture, new Vector2(64, 64), CursorMode.Auto);
        }
    }
}

