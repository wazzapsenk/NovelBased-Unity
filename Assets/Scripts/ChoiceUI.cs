using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textChoice;
    [SerializeField] private Image imageBackground;
    [SerializeField] private Button buttonChoice;
    [SerializeField] private Sprite selectedChoiceBackground;
    [SerializeField] private Sprite unselectedChoiceBackground;
    [SerializeField] private Sprite normalChoiceBackground;


    private void Start()
    {
        buttonChoice.onClick.AddListener(ChoiceSelection);
        imageBackground.sprite = normalChoiceBackground;
    }

    private void ChoiceSelection()
    {
        imageBackground.sprite = selectedChoiceBackground;
    }

    private void OnDestroy()
    {
        buttonChoice.onClick.RemoveListener(ChoiceSelection);
    }

   
}
