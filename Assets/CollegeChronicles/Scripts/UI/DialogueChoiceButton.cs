using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CollegeChronicles.Data;

namespace CollegeChronicles.UI
{
    [RequireComponent(typeof(Button))]
    public class DialogueChoiceButton : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI choiceText;
        public Button button;
        
        [Header("Visual States")]
        public Color normalColor = Color.white;
        public Color hoverColor = Color.cyan;
        public Color selectedColor = Color.green;
        
        [Header("Animation")]
        public float hoverScale = 1.05f;
        public float animationDuration = 0.2f;
        
        private ChoiceData _choiceData;
        private Image _buttonImage;
        private Vector3 _originalScale;
        private bool _isSelected = false;
        
        public System.Action<ChoiceData> OnChoiceSelected;
        
        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
                
            _buttonImage = GetComponent<Image>();
            _originalScale = transform.localScale;
            
            // Set up button events
            button.onClick.AddListener(OnButtonClicked);
            
            // Add hover effects
            var trigger = gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
            // Mouse enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((data) => OnPointerEnter());
            trigger.triggers.Add(enterEntry);
            
            // Mouse exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) => OnPointerExit());
            trigger.triggers.Add(exitEntry);
        }
        
        public void Initialize(ChoiceData choiceData)
        {
            _choiceData = choiceData;
            
            if (choiceText != null && choiceData != null)
            {
                choiceText.text = choiceData.GetDisplayText();
            }
            
            // Set initial visual state
            SetVisualState(normalColor, _originalScale);
        }
        
        private void OnButtonClicked()
        {
            if (_isSelected || _choiceData == null) return;
            
            _isSelected = true;
            
            // Visual feedback for selection
            SetVisualState(selectedColor, _originalScale * 0.95f);
            
            // Slight delay for visual feedback
            DOVirtual.DelayedCall(0.1f, () => {
                OnChoiceSelected?.Invoke(_choiceData);
            });
        }
        
        private void OnPointerEnter()
        {
            if (_isSelected) return;
            
            SetVisualState(hoverColor, _originalScale * hoverScale);
        }
        
        private void OnPointerExit()
        {
            if (_isSelected) return;
            
            SetVisualState(normalColor, _originalScale);
        }
        
        private void SetVisualState(Color color, Vector3 scale)
        {
            // Animate color change
            if (_buttonImage != null)
            {
                _buttonImage.DOColor(color, animationDuration);
            }
            
            // Animate scale change
            transform.DOScale(scale, animationDuration).SetEase(Ease.OutQuad);
        }
        
        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
            
            // Visual feedback for non-interactable state
            if (!interactable)
            {
                var color = normalColor;
                color.a = 0.5f;
                SetVisualState(color, _originalScale * 0.9f);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up any running tweens
            transform.DOKill();
            if (_buttonImage != null)
                _buttonImage.DOKill();
        }
    }
}