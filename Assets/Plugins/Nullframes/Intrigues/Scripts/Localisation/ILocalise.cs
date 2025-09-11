using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nullframes.Intrigues.Localisation.IE
{
    [AddComponentMenu("Intrigues/Localise")]
    public class ILocalise : MonoBehaviour
    {
        private TextMeshProUGUI textMeshProUGUI;
        private TextMeshPro textMeshPro;
        private Text textLegacy;

        public string displayText;

        private void Start()
        {
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            textMeshPro = GetComponent<TextMeshPro>();
            textLegacy = GetComponent<Text>();

            UpdateText();
        }

        private void OnEnable()
        {
            IM.onLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            IM.onLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(string newLanguage)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            var text = IM.GetText(displayText);

            if (textMeshProUGUI != null) textMeshProUGUI.text = text;
            
            if (textMeshPro != null) textMeshPro.text = text;

            if (textLegacy != null) textLegacy.text = text;
        }
    }
}