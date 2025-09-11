using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI
{
    [RequireComponent(typeof(DialoguePanel))]
    public class DragWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private DialoguePanel dialoguePanel;
        private RectTransform dialoguePanelRectTransform;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image backgroundImage;
        private Color backgroundColor;

        private void Awake() {
            dialoguePanel = GetComponent<DialoguePanel>();
            dialoguePanelRectTransform = GetComponent<RectTransform>();
            backgroundColor = backgroundImage.color;
        }

        public void OnDrag(PointerEventData eventData)
        {
            dialoguePanelRectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            backgroundColor.a = .95f;
            backgroundImage.color = backgroundColor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            backgroundColor.a = 1f;
            backgroundImage.color = backgroundColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dialoguePanel.transform.SetAsLastSibling();
        }
    }
}