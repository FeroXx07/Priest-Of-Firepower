using UnityEngine;

namespace _Scripts.UI
{
    public class UIPlayerCanvas : MonoBehaviour
    {

        // Update is called once per frame
        void Update()
        {
            if (transform.parent.gameObject.transform.localScale.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                transform.localScale = new Vector3(1, 1, 1);

            }
        }
    }
}
