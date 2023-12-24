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
        private void FixedUpdate()
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            transform.position = mouseWorldPos; 
        }

        // void Update()
        // {
        //
        //     
        // }
    }
}
