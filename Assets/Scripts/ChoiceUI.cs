using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textChoice;
    [SerializeField] private TextMeshProUGUI textImpact;
    [SerializeField] private Image imageBackground;
    [SerializeField] private Button buttonChoice;
    [SerializeField] private Sprite selectedChoiceBackground;
    [SerializeField] private Sprite unselectedChoiceBackground;
    [SerializeField] private Sprite disabledChoiceBackground;
    
    [SerializeField] private bool canBeSelected = true;
    [SerializeField] private bool isImpactVisible = true;
    [SerializeField] private bool isPositiveImpact = true;


    private void Start()
    {
        if (!canBeSelected)
        {
            buttonChoice.interactable = false;
        }
        buttonChoice.onClick.AddListener(ChoiceSelection);
        imageBackground.sprite = unselectedChoiceBackground;
        textImpact.color = isPositiveImpact ? Color.green : Color.red;
        if (!isImpactVisible)
        {
            textImpact.gameObject.SetActive(false);
        }
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
