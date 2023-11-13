using UnityEngine;

namespace _Scripts
{
    public class CursorScript : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            transform.position = mouseWorldPos; 
        }
    }
}
