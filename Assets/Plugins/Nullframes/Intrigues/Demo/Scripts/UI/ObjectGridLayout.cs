using UnityEngine;

namespace Nullframes.Intrigues.Demo
{
    [ExecuteInEditMode]
    public class ObjectGridLayout : MonoBehaviour
    {
        public float spacing = 1f; // Spacing between child objects

        private void Update()
        {
            SortChildrenHorizontally();
        }

        private void SortChildrenHorizontally()
        {
            int childCount = transform.childCount;
            if (childCount == 0) return;

            // Determine the starting position and spacing to sort child objects horizontally
            float totalWidth = (childCount - 1) * spacing; // Total width
            float startX = -totalWidth * 0.5f; // Starting position

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Vector3 newPosition = new Vector3(startX + i * spacing, child.localPosition.y, child.localPosition.z);
                child.localPosition = newPosition;
            }
        }
    }
}