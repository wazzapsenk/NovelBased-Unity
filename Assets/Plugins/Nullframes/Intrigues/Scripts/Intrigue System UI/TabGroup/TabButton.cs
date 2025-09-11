using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI
{
    [RequireComponent(typeof(Image))]
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public TabGroup tabGroup;

        public Image background;

        public UnityEvent onTabSelected;
        public UnityEvent onTabDeselected;

        private void Start()
        {
            background = GetComponent<Image>();
            tabGroup.Subscribe(this);
        }

        public void Select()
        {
            onTabSelected?.Invoke();
        }

        public void Deselect()
        {
            onTabDeselected?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            tabGroup.OnTabSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tabGroup.OnTabEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tabGroup.OnTabExit(this);
        }

        private void OnDisable() {
            var first = tabGroup.tabButtons.FirstOrDefault(b => b.gameObject.activeSelf);
            if (first == null) return;
            tabGroup.OnTabSelectedWithoutNotify(first);
        }
    }
}