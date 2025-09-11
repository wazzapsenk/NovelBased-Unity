using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using CollegeChronicles.Core;
using CollegeChronicles.Dialogue;
using CollegeChronicles.Telemetry;

namespace CollegeChronicles.Minigame
{
    public class RhythmDanceMinigame : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject minigamePanel;
        public TextMeshProUGUI instructionsText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI sequenceText;
        public Button skipButton;
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        public Button continueButton;
        
        [Header("Arrow Indicators")]
        public Image upArrowIndicator;
        public Image downArrowIndicator;
        public Image leftArrowIndicator;
        public Image rightArrowIndicator;
        
        [Header("Visual Feedback")]
        public Color normalArrowColor = Color.white;
        public Color activeArrowColor = Color.yellow;
        public Color correctArrowColor = Color.green;
        public Color incorrectArrowColor = Color.red;
        
        [Header("Game Settings")]
        public float stepDuration = 1.5f;
        public float inputWindowDuration = 0.8f;
        public float successThreshold = 0.6f; // 60%
        public int sequenceLength = 5;
        
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip correctInputSound;
        public AudioClip incorrectInputSound;
        public AudioClip backgroundMusic;
        
        private DialogueManager _dialogueManager;
        private TelemetryManager _telemetryManager;
        
        private List<KeyCode> _sequence = new List<KeyCode>();
        private List<KeyCode> _validKeys = new List<KeyCode> { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
        private Dictionary<KeyCode, Image> _keyToArrow;
        
        private int _currentStep = 0;
        private int _correctInputs = 0;
        private bool _isWaitingForInput = false;
        private bool _isMinigameActive = false;
        private bool _wasSkipped = false;
        private float _currentAccuracy = 0f;
        
        public System.Action<bool, float, bool> OnMinigameCompleted; // success, accuracy, wasSkipped
        
        private void Awake()
        {
            // Initialize arrow mapping
            _keyToArrow = new Dictionary<KeyCode, Image>
            {
                { KeyCode.UpArrow, upArrowIndicator },
                { KeyCode.DownArrow, downArrowIndicator },
                { KeyCode.LeftArrow, leftArrowIndicator },
                { KeyCode.RightArrow, rightArrowIndicator }
            };
            
            // Set up button listeners
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipButtonClicked);
                
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueButtonClicked);
            
            // Initialize UI state
            SetMinigameVisibility(false);
        }
        
        private void Start()
        {
            // Get manager references
            _dialogueManager = ServiceLocator.Instance?.GetService<DialogueManager>();
            _telemetryManager = ServiceLocator.Instance?.GetService<TelemetryManager>();
            
            if (_dialogueManager != null)
            {
                _dialogueManager.OnMinigameStarted += StartMinigame;
            }
        }
        
        private void OnDestroy()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnMinigameStarted -= StartMinigame;
            }
        }
        
        private void Update()
        {
            if (!_isMinigameActive || !_isWaitingForInput) return;
            
            // Check for arrow key input
            foreach (var key in _validKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    ProcessInput(key);
                    return;
                }
            }
        }
        
        public void StartMinigame()
        {
            Debug.Log("RhythmDanceMinigame: Starting minigame");
            
            _isMinigameActive = true;
            _wasSkipped = false;
            _currentStep = 0;
            _correctInputs = 0;
            _currentAccuracy = 0f;
            
            SetMinigameVisibility(true);
            GenerateSequence();
            StartCoroutine(RunMinigameSequence());
        }
        
        private void GenerateSequence()
        {
            _sequence.Clear();
            
            for (int i = 0; i < sequenceLength; i++)
            {
                var randomKey = _validKeys[Random.Range(0, _validKeys.Count)];
                _sequence.Add(randomKey);
            }
            
            Debug.Log($"Generated sequence: {string.Join(", ", _sequence)}");
            UpdateSequenceDisplay();
        }
        
        private void UpdateSequenceDisplay()
        {
            if (sequenceText == null) return;
            
            var displayText = "Sequence: ";
            for (int i = 0; i < _sequence.Count; i++)
            {
                var arrow = GetArrowSymbol(_sequence[i]);
                if (i == _currentStep)
                {
                    displayText += $"<color=yellow><b>{arrow}</b></color> ";
                }
                else if (i < _currentStep)
                {
                    displayText += $"<color=green>{arrow}</color> ";
                }
                else
                {
                    displayText += $"{arrow} ";
                }
            }
            
            sequenceText.text = displayText;
        }
        
        private string GetArrowSymbol(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                default: return "?";
            }
        }
        
        private IEnumerator RunMinigameSequence()
        {
            if (instructionsText != null)
            {
                instructionsText.text = "Press the arrow keys as they appear!";
            }
            
            // Start background music
            if (audioSource != null && backgroundMusic != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            for (_currentStep = 0; _currentStep < _sequence.Count; _currentStep++)
            {
                if (!_isMinigameActive) yield break; // Exit if skipped or stopped
                
                var currentKey = _sequence[_currentStep];
                
                // Highlight current arrow
                HighlightArrow(currentKey, true);
                UpdateSequenceDisplay();
                
                // Wait for input
                _isWaitingForInput = true;
                float inputTimer = 0f;
                bool inputReceived = false;
                
                while (inputTimer < inputWindowDuration && !inputReceived)
                {
                    inputTimer += Time.deltaTime;
                    inputReceived = !_isWaitingForInput;
                    yield return null;
                }
                
                // If no input received, mark as incorrect
                if (!inputReceived)
                {
                    ProcessInput(KeyCode.None); // No input
                }
                
                // Un-highlight arrow
                HighlightArrow(currentKey, false);
                
                // Short pause between steps
                yield return new WaitForSeconds(0.3f);
            }
            
            CompleteMinigame();
        }
        
        private void ProcessInput(KeyCode inputKey)
        {
            if (!_isWaitingForInput || _currentStep >= _sequence.Count) return;
            
            _isWaitingForInput = false;
            var expectedKey = _sequence[_currentStep];
            bool isCorrect = inputKey == expectedKey;
            
            if (isCorrect)
            {
                _correctInputs++;
                ShowArrowFeedback(expectedKey, true);
                PlaySound(correctInputSound);
            }
            else
            {
                ShowArrowFeedback(expectedKey, false);
                PlaySound(incorrectInputSound);
            }
            
            _currentAccuracy = (float)_correctInputs / (_currentStep + 1);
            UpdateAccuracyDisplay();
            
            Debug.Log($"Step {_currentStep + 1}: Expected {expectedKey}, Got {inputKey}, Correct: {isCorrect}");
        }
        
        private void HighlightArrow(KeyCode key, bool highlight)
        {
            if (_keyToArrow.TryGetValue(key, out Image arrow))
            {
                arrow.color = highlight ? activeArrowColor : normalArrowColor;
                
                if (highlight)
                {
                    arrow.transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad);
                }
                else
                {
                    arrow.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
                }
            }
        }
        
        private void ShowArrowFeedback(KeyCode key, bool correct)
        {
            if (_keyToArrow.TryGetValue(key, out Image arrow))
            {
                var feedbackColor = correct ? correctArrowColor : incorrectArrowColor;
                arrow.color = feedbackColor;
                
                // Brief feedback animation
                var sequence = DOTween.Sequence();
                sequence.Append(arrow.transform.DOScale(1.5f, 0.1f));
                sequence.Append(arrow.transform.DOScale(1f, 0.2f));
                sequence.OnComplete(() => arrow.color = normalArrowColor);
            }
        }
        
        private void UpdateAccuracyDisplay()
        {
            if (accuracyText != null)
            {
                var percentage = Mathf.RoundToInt(_currentAccuracy * 100f);
                accuracyText.text = $"Accuracy: {percentage}%";
            }
        }
        
        private void CompleteMinigame()
        {
            _isMinigameActive = false;
            _isWaitingForInput = false;
            
            // Stop background music
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            
            var finalAccuracy = _currentAccuracy;
            var isSuccess = finalAccuracy >= successThreshold && !_wasSkipped;
            
            Debug.Log($"Minigame completed: Success: {isSuccess}, Accuracy: {finalAccuracy:P}, Skipped: {_wasSkipped}");
            
            // Log telemetry
            _telemetryManager?.LogMinigameResult("rhythm_dance", isSuccess, finalAccuracy, _wasSkipped);
            
            ShowResults(isSuccess, finalAccuracy);
            OnMinigameCompleted?.Invoke(isSuccess, finalAccuracy, _wasSkipped);
        }
        
        private void ShowResults(bool success, float accuracy)
        {
            if (resultPanel == null || resultText == null) return;
            
            string resultMessage;
            if (_wasSkipped)
            {
                resultMessage = "Minigame Skipped";
            }
            else if (success)
            {
                resultMessage = $"Success!\nAccuracy: {Mathf.RoundToInt(accuracy * 100)}%\n\nGreat rhythm! The crowd cheers!";
            }
            else
            {
                resultMessage = $"Not quite...\nAccuracy: {Mathf.RoundToInt(accuracy * 100)}%\n\nBetter luck next time!";
            }
            
            resultText.text = resultMessage;
            resultPanel.SetActive(true);
            
            // Animate result panel
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
        
        private void OnSkipButtonClicked()
        {
            Debug.Log("RhythmDanceMinigame: Skip button clicked");
            
            _wasSkipped = true;
            _isMinigameActive = false;
            _isWaitingForInput = false;
            
            StopAllCoroutines();
            CompleteMinigame();
        }
        
        private void OnContinueButtonClicked()
        {
            SetMinigameVisibility(false);
            
            // Resume dialogue system
            if (_dialogueManager != null)
            {
                // The dialogue manager will handle continuing the story based on minigame result
                _dialogueManager.ResumeDialogue();
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        private void SetMinigameVisibility(bool visible)
        {
            if (minigamePanel != null)
            {
                minigamePanel.SetActive(visible);
            }
            
            if (resultPanel != null && !visible)
            {
                resultPanel.SetActive(false);
            }
        }
        
        // Public getters for external systems
        public bool IsMinigameActive => _isMinigameActive;
        public float CurrentAccuracy => _currentAccuracy;
        public bool WasSuccessful => _currentAccuracy >= successThreshold && !_wasSkipped;
        
        // Debug utilities
        [ContextMenu("Test Start Minigame")]
        private void Debug_StartMinigame()
        {
            StartMinigame();
        }
        
        [ContextMenu("Test Skip Minigame")]
        private void Debug_SkipMinigame()
        {
            OnSkipButtonClicked();
        }
    }
}