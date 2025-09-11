using UnityEngine;

namespace Nullframes.Intrigues
{
    public class RemoveEvent : MonoBehaviour
    {
        public void DestroyGameObject()
        {
            Destroy(gameObject);
        }
    }
}