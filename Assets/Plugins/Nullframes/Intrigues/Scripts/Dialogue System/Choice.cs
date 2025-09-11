using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI
{
    [RequireComponent(typeof(Button))]
    public class Choice : IIEditor
    {
        public TextMeshProUGUI text;
        public Button button;
        public Image icon;
    }
}