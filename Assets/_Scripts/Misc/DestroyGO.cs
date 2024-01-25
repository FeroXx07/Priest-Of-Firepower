using UnityEngine;

namespace _Scripts.Misc
{
    public class DestroyGO : MonoBehaviour
    {
        public void DestroyGameObject()
        {
            Destroy(this.gameObject);
        }
    }
}
