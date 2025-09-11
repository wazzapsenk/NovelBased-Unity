using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI
{
    public class Tooltip : MonoBehaviour
    {
        public TextMeshProUGUI headerField;
        public TextMeshProUGUI contentField;

        public LayoutElement layoutElement;

        public int characterWrapLimit;

        [SerializeField] private RectTransform bgRect;
        private RectTransform rectTransform;

        [SerializeField] private CanvasGroup canvasGroup;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            canvasGroup.alpha = 0f;
        }

        public void SetText(string content, string header = "")
        {
            if (string.IsNullOrEmpty(header))
            {
                headerField.gameObject.SetActive(false);
            }
            else
            {
                headerField.gameObject.SetActive(true);
                headerField.text = header;
            }

            contentField.text = content;

            var headerLength = headerField.text.Length;
            var contentLength = contentField.text.Length;

            layoutElement.enabled = headerLength > characterWrapLimit || contentLength > characterWrapLimit;
        }

        private void Update()
        {
            if (canvasGroup.alpha < 1f) canvasGroup.alpha += 8f * Time.deltaTime;

            var tooltipPosition = Input.mousePosition;

            var tooltipWidth = bgRect.rect.width;
            var tooltipHeight = bgRect.rect.height;

            if (tooltipPosition.x + tooltipWidth > Screen.width) tooltipPosition.x = Screen.width - tooltipWidth;

            if (tooltipPosition.y + tooltipHeight > Screen.height) tooltipPosition.y = Screen.height - tooltipHeight;

            rectTransform.position = tooltipPosition;
        }
    }
}