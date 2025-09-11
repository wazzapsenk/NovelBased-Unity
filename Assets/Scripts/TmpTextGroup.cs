using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


    public class TmpTextGroup : MonoBehaviour
    {
        [SerializeField] private bool resizeOnStart = true;
        [SerializeField] private List<TMP_Text> tmpTexts;

        private void Start()
        {
            if (resizeOnStart)
            {
                Resize();
            }
        }

        [Button]
        public void Resize()
        {
            if (tmpTexts == null || tmpTexts.Count == 0)
                return;

            if (tmpTexts.Count == 1)
            {
                ResizeSingle();
                return;
            }

            var length = int.MinValue;
            TMP_Text tmpTextWithMaxLength = null;
            foreach (var tmpText in tmpTexts)
            {
                tmpText.enableAutoSizing = false;

                // assume we're using a monospace font and "iiii" is wider than "WWW"
                var tmpTextLength = tmpText.GetParsedText().Length;
                if (tmpTextLength > length)
                {
                    length = tmpTextLength;
                    tmpTextWithMaxLength = tmpText;
                }
            }

            if (tmpTextWithMaxLength == null)
                return;

            tmpTextWithMaxLength.enableAutoSizing = true;
            tmpTextWithMaxLength.ForceMeshUpdate();

            var fontSize = tmpTextWithMaxLength.fontSize;
            tmpTextWithMaxLength.enableAutoSizing = false;

            foreach (var tmpText in tmpTexts)
            {
                tmpText.fontSize = fontSize;
            }
        }

        private void ResizeSingle()
        {
            var tmpText = tmpTexts[0];

            tmpText.enableAutoSizing = true;
            tmpText.ForceMeshUpdate(ignoreActiveState: true);

            var newFontSize = tmpText.fontSize;
            tmpText.enableAutoSizing = false;

            tmpText.fontSize = newFontSize;
        }

        public void AddTmpText(TMP_Text tmpText)
        {
            if (!tmpTexts.Contains(tmpText))
            {
                tmpTexts.Add(tmpText);
            }
        }

        public void ClearTmpTexts()
        {
            tmpTexts.Clear();
        }
    }

