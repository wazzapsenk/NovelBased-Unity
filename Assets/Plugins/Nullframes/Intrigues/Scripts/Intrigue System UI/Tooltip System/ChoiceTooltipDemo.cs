using UnityEngine.EventSystems;

namespace Nullframes.Intrigues.UI {
    public class ChoiceTooltipDemo : IChoiceData, IPointerEnterHandler {
        public TooltipTrigger tooltipTrigger;
        private bool disabled;

        public void OnPointerEnter(PointerEventData eventData) {
            if (disabled) return;
            
            ChoiceData data = ChoiceData;
            
            tooltipTrigger.ClearTooltip();

            if (data == null)
                return;

            if (data.Rate != null) {
                tooltipTrigger.content =
                    $"Success Rate: {data.Rate.SuccessRate:F1}\nFail Rate: {data.Rate.FailRate:F1}";
            }

            if (!string.IsNullOrEmpty(data.Text1)) {
                if (data.Rate != null) {
                    tooltipTrigger.content += "\n\n";
                }

                tooltipTrigger.content += data.Text1;
            }
        }

        public override void OnDisabled() {
            disabled = true;
            
            tooltipTrigger.ClearTooltip();
            tooltipTrigger.header = "You cannot do that.";
            tooltipTrigger.content = ChoiceData.Text2;
        }

        public override void OnEnabled() {
            disabled = false;
        }
    }
}