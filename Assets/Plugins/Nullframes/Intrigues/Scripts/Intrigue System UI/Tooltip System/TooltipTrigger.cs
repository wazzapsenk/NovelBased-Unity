using UnityEngine;
using UnityEngine.EventSystems;

namespace Nullframes.Intrigues.UI
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public string header;
        [Multiline] public string content;

        private string delay;

        public void ClearTooltip()
        {
            header = string.Empty;
            content = string.Empty;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            delay = NullUtils.DelayedCall(new DelayedCallParams {
                Delay = .2f,
                Call = () => { TooltipSystem.ShowTooltip(content, header); },
                UnscaledTime = true,
            });
            // delay = NullUtils.DelayedCall(.2f, () => { TooltipSystem.ShowTooltip(content, header); }, 0, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            NullUtils.StopCall(delay);
            TooltipSystem.HideTooltip();
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            NullUtils.StopCall(delay);
            TooltipSystem.HideTooltip();
        }

        private void OnMouseEnter()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            delay = NullUtils.DelayedCall(new DelayedCallParams {
                Delay = .2f,
                Call = () => { TooltipSystem.ShowTooltip(content, header); },
                UnscaledTime = true,
            });
            
            // delay = NullUtils.DelayedCall(.2f, () => { TooltipSystem.ShowTooltip(content, header); });
        }

        private void OnMouseExit()
        {
            NullUtils.StopCall(delay);
            TooltipSystem.HideTooltip();
        }

        private void OnMouseDown()
        {
            NullUtils.StopCall(delay);
            TooltipSystem.HideTooltip();
        }
    }
}