using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using CollegeChronicles.Core;

namespace CollegeChronicles.Navigation
{
    [RequireComponent(typeof(Button))]
    public class LocationTrigger : MonoBehaviour
    {
        [Header("Target Configuration")]
        public string targetSceneName;
        public string locationDisplayName;
        
        [Header("Visual Feedback")]
        public Image backgroundImage;
        public Color normalColor = Color.white;
        public Color hoverColor = Color.cyan;
        public Color unavailableColor = Color.gray;
        
        [Header("Animation")]
        public float hoverScale = 1.1f;
        public float animationDuration = 0.3f;
        
        [Header("Availability")]
        public bool isAlwaysAvailable = true;
        public bool requiresFreeRoam = true;
        
        private Button _button;
        private NavigationManager _navigationManager;
        private Vector3 _originalScale;
        private bool _isHovering = false;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
            
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
                
            _button.onClick.AddListener(OnLocationClicked);
            
            // Add hover effects
            var trigger = GetComponent<UnityEngine.EventSystems.EventTrigger>();
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
        
        private void Start()
        {
            _navigationManager = ServiceLocator.Instance?.GetService<NavigationManager>();
            
            if (_navigationManager != null)
            {
                _navigationManager.OnFreeRoamStarted += OnFreeRoamStarted;
                _navigationManager.OnFreeRoamEnded += OnFreeRoamEnded;
            }
            
            UpdateAvailability();
        }
        
        private void OnDestroy()
        {
            if (_navigationManager != null)
            {
                _navigationManager.OnFreeRoamStarted -= OnFreeRoamStarted;
                _navigationManager.OnFreeRoamEnded -= OnFreeRoamEnded;
            }
            
            // Clean up tweens
            transform.DOKill();
            if (backgroundImage != null)
                backgroundImage.DOKill();
        }
        
        private void OnLocationClicked()
        {
            if (!IsAvailable())
            {
                Debug.LogWarning($"LocationTrigger: Cannot navigate to '{targetSceneName}' - not available");
                return;
            }
            
            if (_navigationManager == null)
            {
                Debug.LogError("LocationTrigger: NavigationManager not found");
                return;
            }
            
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("LocationTrigger: Target scene name is not set");
                return;
            }
            
            Debug.Log($"LocationTrigger: Navigating to '{targetSceneName}' ({locationDisplayName})");
            
            // Visual feedback for click
            AnimateClick();
            
            // Navigate after short delay for visual feedback
            DOVirtual.DelayedCall(0.2f, () => {
                _navigationManager.LoadScene(targetSceneName);
            });
        }
        
        private void OnPointerEnter()
        {
            if (!IsAvailable()) return;
            
            _isHovering = true;
            AnimateHoverIn();
        }
        
        private void OnPointerExit()
        {
            _isHovering = false;
            AnimateHoverOut();
        }
        
        private void AnimateHoverIn()
        {
            if (backgroundImage != null)
            {
                backgroundImage.DOColor(hoverColor, animationDuration);
            }
            
            transform.DOScale(_originalScale * hoverScale, animationDuration)
                .SetEase(Ease.OutQuad);
        }
        
        private void AnimateHoverOut()
        {
            var targetColor = IsAvailable() ? normalColor : unavailableColor;
            
            if (backgroundImage != null)
            {
                backgroundImage.DOColor(targetColor, animationDuration);
            }
            
            transform.DOScale(_originalScale, animationDuration)
                .SetEase(Ease.OutQuad);
        }
        
        private void AnimateClick()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_originalScale * 0.95f, 0.1f));
            sequence.Append(transform.DOScale(_originalScale * hoverScale, 0.1f));
        }
        
        private void OnFreeRoamStarted()
        {
            UpdateAvailability();
        }
        
        private void OnFreeRoamEnded()
        {
            UpdateAvailability();
        }
        
        private void UpdateAvailability()
        {
            bool available = IsAvailable();
            
            _button.interactable = available;
            
            var targetColor = available ? (_isHovering ? hoverColor : normalColor) : unavailableColor;
            
            if (backgroundImage != null)
            {
                backgroundImage.DOColor(targetColor, animationDuration);
            }
        }
        
        public bool IsAvailable()
        {
            if (!isAlwaysAvailable) return false;
            
            if (requiresFreeRoam && (_navigationManager == null || !_navigationManager.IsFreeRoamActive))
                return false;
                
            if (_navigationManager != null && !_navigationManager.CanNavigateTo(targetSceneName))
                return false;
                
            return true;
        }
        
        public void SetAvailability(bool available)
        {
            isAlwaysAvailable = available;
            UpdateAvailability();
        }
        
        // Editor utility
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(locationDisplayName) && !string.IsNullOrEmpty(targetSceneName))
            {
                locationDisplayName = targetSceneName;
            }
        }
    }
}