using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using CollegeChronicles.Core;
using CollegeChronicles.Dialogue;
using CollegeChronicles.Navigation;

namespace CollegeChronicles.UI
{
    public class SmartphoneUI_Controller : MonoBehaviour
    {
        [Header("Phone Notification")]
        public GameObject phoneNotificationIcon;
        public Button phoneButton;
        
        [Header("Messaging Interface")]
        public GameObject messagingPanel;
        public Transform messageContainer;
        public GameObject messageBubblePrefab;
        public Button closeMessagingButton;
        
        [Header("Animation Settings")]
        public float vibrationIntensity = 10f;
        public float vibrationDuration = 2f;
        public int vibrationCount = 3;
        public float panelAnimationDuration = 0.5f;
        
        [Header("Message Content")]
        public List<MessageData> predefinedMessages = new List<MessageData>
        {
            new MessageData("Sam", "Hey Alex! It's Sam from orientation week."),
            new MessageData("Sam", "Come to the campus quad when you get a chance!"),
            new MessageData("Sam", "There's something happening you don't want to miss 😊")
        };
        
        private DialogueManager _dialogueManager;
        private NavigationManager _navigationManager;
        private Vector3 _originalPhonePosition;
        private bool _isMessagingOpen = false;
        private bool _hasBeenRead = false;
        private Sequence _vibrationSequence;
        
        [System.Serializable]
        public class MessageData
        {
            public string senderName;
            public string messageText;
            public bool isRead = false;
            
            public MessageData(string sender, string message)
            {
                senderName = sender;
                messageText = message;
            }
        }
        
        private void Awake()
        {
            // Store original position for vibration animation
            if (phoneNotificationIcon != null)
            {
                _originalPhonePosition = phoneNotificationIcon.transform.position;
            }
            
            // Set up button listeners
            if (phoneButton != null)
            {
                phoneButton.onClick.AddListener(OnPhoneButtonClicked);
            }
            
            if (closeMessagingButton != null)
            {
                closeMessagingButton.onClick.AddListener(OnCloseMessagingClicked);
            }
            
            // Initialize UI state
            SetPhoneNotificationVisibility(false);
            SetMessagingPanelVisibility(false);
        }
        
        private void Start()
        {
            // Get manager references
            _dialogueManager = ServiceLocator.Instance?.GetService<DialogueManager>();
            _navigationManager = ServiceLocator.Instance?.GetService<NavigationManager>();
            
            if (_dialogueManager != null)
            {
                _dialogueManager.OnPhoneNotificationTriggered += OnPhoneNotificationTriggered;
            }
        }
        
        private void OnDestroy()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnPhoneNotificationTriggered -= OnPhoneNotificationTriggered;
            }
            
            _vibrationSequence?.Kill();
        }
        
        private void OnPhoneNotificationTriggered()
        {
            ShowPhoneNotification();
        }
        
        public void ShowPhoneNotification()
        {
            if (_hasBeenRead)
            {
                Debug.Log("SmartphoneUI: Phone notification already read, skipping");
                return;
            }
            
            Debug.Log("SmartphoneUI: Showing phone notification with vibration");
            
            SetPhoneNotificationVisibility(true);
            StartVibrationAnimation();
        }
        
        private void StartVibrationAnimation()
        {
            if (phoneNotificationIcon == null) return;
            
            _vibrationSequence?.Kill();
            _vibrationSequence = DOTween.Sequence();
            
            for (int i = 0; i < vibrationCount; i++)
            {
                _vibrationSequence.Append(
                    phoneNotificationIcon.transform.DOShakePosition(
                        vibrationDuration / vibrationCount, 
                        vibrationIntensity, 
                        10, 
                        90, 
                        false, 
                        true
                    )
                );
                _vibrationSequence.AppendInterval(0.2f);
            }
            
            _vibrationSequence.OnComplete(() => {
                phoneNotificationIcon.transform.position = _originalPhonePosition;
            });
        }
        
        private void OnPhoneButtonClicked()
        {
            if (_isMessagingOpen) return;
            
            Debug.Log("SmartphoneUI: Phone button clicked, opening messaging interface");
            
            _hasBeenRead = true;
            SetPhoneNotificationVisibility(false);
            OpenMessagingInterface();
        }
        
        private void OpenMessagingInterface()
        {
            _isMessagingOpen = true;
            
            // Create message bubbles
            CreateMessageBubbles();
            
            // Animate panel in
            SetMessagingPanelVisibility(true);
            AnimateMessagingPanelIn();
        }
        
        private void CreateMessageBubbles()
        {
            if (messageContainer == null || messageBubblePrefab == null) return;
            
            // Clear existing messages
            foreach (Transform child in messageContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new message bubbles
            for (int i = 0; i < predefinedMessages.Count; i++)
            {
                var messageData = predefinedMessages[i];
                var messageBubble = Instantiate(messageBubblePrefab, messageContainer);
                
                var messageText = messageBubble.GetComponentInChildren<TextMeshProUGUI>();
                if (messageText != null)
                {
                    messageText.text = $"<b>{messageData.senderName}:</b>\n{messageData.messageText}";
                }
                
                // Animate message bubble in with delay
                messageBubble.transform.localScale = Vector3.zero;
                messageBubble.transform.DOScale(1f, 0.3f)
                    .SetDelay(i * 0.2f)
                    .SetEase(Ease.OutBack);
                    
                messageData.isRead = true;
            }
        }
        
        private void AnimateMessagingPanelIn()
        {
            if (messagingPanel == null) return;
            
            // Start from off-screen (bottom)
            var canvasRect = messagingPanel.GetComponent<RectTransform>();
            var startPos = canvasRect.anchoredPosition;
            startPos.y = -Screen.height;
            canvasRect.anchoredPosition = startPos;
            
            // Animate to center
            canvasRect.DOAnchorPosY(0, panelAnimationDuration)
                .SetEase(Ease.OutQuart);
                
            // Fade in
            var canvasGroup = messagingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = messagingPanel.AddComponent<CanvasGroup>();
                
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, panelAnimationDuration * 0.7f);
        }
        
        private void OnCloseMessagingClicked()
        {
            if (!_isMessagingOpen) return;
            
            Debug.Log("SmartphoneUI: Closing messaging interface");
            
            CloseMessagingInterface();
        }
        
        private void CloseMessagingInterface()
        {
            if (messagingPanel == null) return;
            
            var canvasRect = messagingPanel.GetComponent<RectTransform>();
            var canvasGroup = messagingPanel.GetComponent<CanvasGroup>();
            
            // Animate out
            var sequence = DOTween.Sequence();
            
            if (canvasGroup != null)
            {
                sequence.Append(canvasGroup.DOFade(0f, panelAnimationDuration * 0.3f));
            }
            
            sequence.Append(canvasRect.DOAnchorPosY(-Screen.height, panelAnimationDuration)
                .SetEase(Ease.InQuart));
                
            sequence.OnComplete(() => {
                _isMessagingOpen = false;
                SetMessagingPanelVisibility(false);
                
                // Continue dialogue or trigger next story beat
                OnMessagingClosed();
            });
        }
        
        private void OnMessagingClosed()
        {
            Debug.Log("SmartphoneUI: Messaging closed, ready for next story beat");
            
            // This could trigger navigation to campus quad or continue dialogue
            // For the prototype, we'll assume the player should head to campus quad
        }
        
        private void SetPhoneNotificationVisibility(bool visible)
        {
            if (phoneNotificationIcon != null)
            {
                phoneNotificationIcon.SetActive(visible);
            }
        }
        
        private void SetMessagingPanelVisibility(bool visible)
        {
            if (messagingPanel != null)
            {
                messagingPanel.SetActive(visible);
            }
        }
        
        public bool IsNotificationActive()
        {
            return phoneNotificationIcon != null && phoneNotificationIcon.activeSelf;
        }
        
        public bool IsMessagingOpen()
        {
            return _isMessagingOpen;
        }
        
        public void ResetNotification()
        {
            _hasBeenRead = false;
            SetPhoneNotificationVisibility(false);
            SetMessagingPanelVisibility(false);
            _isMessagingOpen = false;
        }
        
        // Custom message support for future expansion
        public void SetCustomMessages(List<MessageData> messages)
        {
            predefinedMessages = new List<MessageData>(messages);
        }
        
        public void AddMessage(string sender, string message)
        {
            predefinedMessages.Add(new MessageData(sender, message));
        }
        
        // Debug utilities
        [ContextMenu("Test Phone Notification")]
        private void Debug_TestPhoneNotification()
        {
            ShowPhoneNotification();
        }
        
        [ContextMenu("Reset Notification State")]
        private void Debug_ResetNotification()
        {
            ResetNotification();
        }
    }
}