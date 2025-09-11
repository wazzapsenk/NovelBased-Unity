using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using CollegeChronicles.Data;
using CollegeChronicles.Dialogue;
using CollegeChronicles.Core;

namespace CollegeChronicles.UI
{
    public class DialogueUI_Controller : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        public Image characterPortrait;
        public Transform choicesContainer;
        public GameObject choiceButtonPrefab;
        public Button continueButton;
        
        [Header("Stats Display")]
        public TextMeshProUGUI statsText;
        
        [Header("Feedback")]
        public GameObject personalityFeedbackPrefab;
        public Transform feedbackContainer;
        
        [Header("Animation Settings")]
        public float textRevealSpeed = 50f;
        public float feedbackAnimationDuration = 2f;
        
        private DialogueManager _dialogueManager;
        private PlayerStatsManager _statsManager;
        private List<DialogueChoiceButton> _currentChoiceButtons = new List<DialogueChoiceButton>();
        private bool _isRevealingText = false;
        private Sequence _textRevealSequence;
        
        private void Awake()
        {
            // Initialize UI state
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
                
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                continueButton.gameObject.SetActive(false);
            }
        }
        
        private void Start()
        {
            // Get manager references
            _dialogueManager = ServiceLocator.Instance?.GetService<DialogueManager>();
            _statsManager = ServiceLocator.Instance?.GetService<PlayerStatsManager>();
            
            if (_dialogueManager == null)
            {
                Debug.LogError("DialogueUI_Controller: DialogueManager not found in ServiceLocator");
                return;
            }
            
            // Subscribe to events
            _dialogueManager.OnDialogueNodeChanged += OnDialogueNodeChanged;
            _dialogueManager.OnChoicesPresented += OnChoicesPresented;
            _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            
            if (_statsManager != null)
            {
                _statsManager.OnStatsChanged += OnStatsChanged;
                _statsManager.OnPersonalityImpacted += OnPersonalityImpacted;
                
                // Initialize stats display
                OnStatsChanged(_statsManager.AlphaScore, _statsManager.BetaScore);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueNodeChanged -= OnDialogueNodeChanged;
                _dialogueManager.OnChoicesPresented -= OnChoicesPresented;
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
            }
            
            if (_statsManager != null)
            {
                _statsManager.OnStatsChanged -= OnStatsChanged;
                _statsManager.OnPersonalityImpacted -= OnPersonalityImpacted;
            }
            
            _textRevealSequence?.Kill();
        }
        
        private void OnDialogueNodeChanged(DialogueNode node)
        {
            if (node == null) return;
            
            ShowDialoguePanel();
            DisplayNode(node);
        }
        
        private void DisplayNode(DialogueNode node)
        {
            // Update speaker name
            if (speakerNameText != null)
            {
                speakerNameText.text = node.speakerName;
            }
            
            // Update character portrait
            UpdateCharacterPortrait(node.speakerName, node.characterExpression);
            
            // Animate text reveal
            RevealDialogueText(node.dialogueText);
            
            // Hide choices and continue button initially
            ClearChoices();
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }
        
        private void UpdateCharacterPortrait(string characterName, string expression)
        {
            if (characterPortrait == null || _dialogueManager?.dialogueDatabase == null) return;
            
            var characterData = _dialogueManager.dialogueDatabase.GetCharacter(characterName);
            if (characterData != null)
            {
                var sprite = characterData.GetExpression(expression);
                characterPortrait.sprite = sprite;
                characterPortrait.gameObject.SetActive(sprite != null);
            }
            else
            {
                characterPortrait.gameObject.SetActive(false);
            }
        }
        
        private void RevealDialogueText(string text)
        {
            if (dialogueText == null) return;
            
            _isRevealingText = true;
            dialogueText.text = "";
            
            _textRevealSequence?.Kill();
            _textRevealSequence = DOTween.Sequence();
            
            _textRevealSequence.Append(
                dialogueText.DOText(text, text.Length / textRevealSpeed)
                    .SetEase(Ease.Linear)
            );
            
            _textRevealSequence.OnComplete(() => {
                _isRevealingText = false;
                OnTextRevealComplete();
            });
        }
        
        private void OnTextRevealComplete()
        {
            var currentNode = _dialogueManager.CurrentNode;
            if (currentNode == null) return;
            
            // Show appropriate UI elements based on node type
            if (currentNode.HasChoices)
            {
                // Choices will be presented via OnChoicesPresented event
            }
            else if (!currentNode.isAutomatic)
            {
                // Show continue button for manual progression
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
            }
        }
        
        private void OnChoicesPresented(List<ChoiceData> choices)
        {
            if (choices == null || choices.Count == 0) return;
            
            CreateChoiceButtons(choices);
        }
        
        private void CreateChoiceButtons(List<ChoiceData> choices)
        {
            ClearChoices();
            
            if (choiceButtonPrefab == null || choicesContainer == null)
            {
                Debug.LogError("DialogueUI_Controller: Choice button prefab or container not assigned");
                return;
            }
            
            foreach (var choice in choices)
            {
                var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                var choiceButton = buttonObj.GetComponent<DialogueChoiceButton>();
                
                if (choiceButton != null)
                {
                    choiceButton.Initialize(choice);
                    choiceButton.OnChoiceSelected += OnChoiceSelected;
                    _currentChoiceButtons.Add(choiceButton);
                }
            }
            
            // Animate choice buttons in
            AnimateChoicesIn();
        }
        
        private void AnimateChoicesIn()
        {
            for (int i = 0; i < _currentChoiceButtons.Count; i++)
            {
                var button = _currentChoiceButtons[i];
                var buttonTransform = button.transform;
                
                // Start with scale 0 and fade in
                buttonTransform.localScale = Vector3.zero;
                button.GetComponent<CanvasGroup>()?.DOFade(0f, 0f);
                
                var sequence = DOTween.Sequence();
                sequence.SetDelay(i * 0.1f); // Stagger the animations
                sequence.Append(buttonTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                sequence.Join(button.GetComponent<CanvasGroup>()?.DOFade(1f, 0.3f));
            }
        }
        
        private void OnChoiceSelected(ChoiceData choice)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.MakeChoice(choice);
            }
        }
        
        private void ClearChoices()
        {
            foreach (var button in _currentChoiceButtons)
            {
                if (button != null)
                {
                    button.OnChoiceSelected -= OnChoiceSelected;
                    Destroy(button.gameObject);
                }
            }
            _currentChoiceButtons.Clear();
        }
        
        private void OnDialogueEnded()
        {
            HideDialoguePanel();
        }
        
        private void ShowDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                dialoguePanel.transform.localScale = Vector3.zero;
                dialoguePanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }
        
        private void HideDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.transform.DOScale(0f, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => dialoguePanel.SetActive(false));
            }
        }
        
        private void OnStatsChanged(int alphaScore, int betaScore)
        {
            if (statsText != null)
            {
                statsText.text = $"Alpha: {alphaScore} | Beta: {betaScore}";
            }
        }
        
        private void OnPersonalityImpacted(PersonalityImpact impact)
        {
            ShowPersonalityFeedback(impact);
        }
        
        private void ShowPersonalityFeedback(PersonalityImpact impact)
        {
            if (personalityFeedbackPrefab == null || feedbackContainer == null) return;
            
            var feedbackObj = Instantiate(personalityFeedbackPrefab, feedbackContainer);
            var feedbackText = feedbackObj.GetComponent<TextMeshProUGUI>();
            
            if (feedbackText != null)
            {
                feedbackText.text = impact.ToString();
                
                // Animate feedback
                var sequence = DOTween.Sequence();
                sequence.Append(feedbackObj.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutQuad));
                sequence.Append(feedbackObj.transform.DOScale(1f, 0.1f));
                sequence.AppendInterval(feedbackAnimationDuration - 0.8f);
                sequence.Append(feedbackText.DOFade(0f, 0.5f));
                sequence.OnComplete(() => Destroy(feedbackObj));
            }
        }
        
        private void OnContinueButtonClicked()
        {
            if (_isRevealingText)
            {
                // Skip text animation
                _textRevealSequence?.Complete();
            }
            else if (_dialogueManager != null)
            {
                _dialogueManager.ContinueDialogue();
            }
        }
        
        // Public methods for external control
        public void StartDialogue(string sceneID = null)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.StartDialogue(sceneID);
            }
        }
        
        public bool IsDialogueActive()
        {
            return dialoguePanel != null && dialoguePanel.activeSelf;
        }
    }
}