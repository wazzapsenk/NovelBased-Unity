using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nullframes.Intrigues.UI {
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LinkHandler : IIEditor, IPointerClickHandler {
        private TextMeshProUGUI tmpText;

        private void Start() {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
        
        public void OnPointerClick(PointerEventData eventData) {
            Vector3 mousePosition = new Vector3(eventData.position.x, eventData.position.y, 0);

            var linkTaggedText = TMP_TextUtilities.FindIntersectingLink(tmpText, mousePosition, null);

            if (linkTaggedText != -1) {
                TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkTaggedText];

                string linkId = linkInfo.GetLinkID();
                Actor actor = null;

                if (IM.ActorDictionary.ContainsKey(linkId)) {
                    actor = IM.ActorDictionary[linkId];
                }
                
                if (IntrigueSystemUI.instance != null) {
                    if (actor != null) {
                        IntrigueSystemUI.instance.OpenActorMenu(actor);
                    }
                }
            }
        }
    }
}