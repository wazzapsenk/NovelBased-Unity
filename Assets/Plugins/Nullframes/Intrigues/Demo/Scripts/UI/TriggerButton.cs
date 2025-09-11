using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Nullframes.Intrigues.Demo
{
    public class TriggerButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        public UnityEvent onMouseClick;
        public UnityEvent onMouseEnter;
        public UnityEvent onMouseExit;
        
        private void OnMouseEnter()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Enter();
        }
        
        private void OnMouseDown()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Click();
        }

        private void OnMouseExit()
        {
            onMouseExit.Invoke();
        }

        private void OnDisable()
        {
            onMouseExit.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Enter();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button == PointerEventData.InputButton.Left)
                Click();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnMouseExit();
        }

        private void Enter()
        {
            onMouseEnter.Invoke();
        }

        private void Click()
        {
            onMouseClick.Invoke();
        }
    }
}