using UnityEngine;

namespace Nullframes.Intrigues.UI
{
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem instance;

        public Tooltip Tooltip;
        
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this);
        }
        
        public static void ShowTooltip(string content, string header = "")
        {
            if (instance == null) return;

            // Check if the content and header are empty or null
            if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(header))
                return;

            // Set the text content and header of the tooltip
            instance.Tooltip.SetText(content, header);

            // Activate the tooltip game object to show it
            instance.Tooltip.gameObject.SetActive(true);
        }

        public static void HideTooltip()
        {
            if (instance == null) return;
            // Deactivate the tooltip game object to hide it
            instance.Tooltip.gameObject.SetActive(false);
        }
    }
}