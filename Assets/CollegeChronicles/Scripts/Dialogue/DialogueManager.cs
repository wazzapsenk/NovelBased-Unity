using UnityEngine;
using System.Collections.Generic;
using CollegeChronicles.Data;
using CollegeChronicles.Core;
using CollegeChronicles.Telemetry;

namespace CollegeChronicles.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("Configuration")]
        public DialogueDatabase dialogueDatabase;
        
        [Header("Current State")]
        [SerializeField] private DialogueNode _currentNode;
        [SerializeField] private string _currentSceneID = "DormRoom";
        
        public System.Action<DialogueNode> OnDialogueNodeChanged;
        public System.Action<List<ChoiceData>> OnChoicesPresented;
        public System.Action OnDialogueEnded;
        public System.Action OnPhoneNotificationTriggered;
        public System.Action OnFreeRoamStarted;
        public System.Action OnMinigameStarted;
        
        private PlayerStatsManager _statsManager;
        private TelemetryManager _telemetryManager;
        
        public DialogueNode CurrentNode => _currentNode;
        public bool IsInDialogue => _currentNode != null;
        
        private void Awake()
        {
            // Register with ServiceLocator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.RegisterService(this);
            }
        }
        
        private void Start()
        {
            // Get references to other managers
            _statsManager = ServiceLocator.Instance?.GetService<PlayerStatsManager>();
            _telemetryManager = ServiceLocator.Instance?.GetService<TelemetryManager>();
            
            if (dialogueDatabase == null)
            {
                Debug.LogError("DialogueManager: No DialogueDatabase assigned!");
                return;
            }
        }
        
        public void StartDialogue(string sceneID = null)
        {
            if (!string.IsNullOrEmpty(sceneID))
            {
                _currentSceneID = sceneID;
            }
            
            var startingNode = dialogueDatabase.GetStartingNode(_currentSceneID);
            if (startingNode == null)
            {
                Debug.LogError($"DialogueManager: No starting node found for scene '{_currentSceneID}'");
                return;
            }
            
            ProcessNode(startingNode);
        }
        
        public void StartDialogueFromNode(DialogueNode node)
        {
            if (node == null)
            {
                Debug.LogError("DialogueManager: Attempted to start dialogue from null node");
                return;
            }
            
            ProcessNode(node);
        }
        
        public void MakeChoice(ChoiceData choice)
        {
            if (choice == null)
            {
                Debug.LogError("DialogueManager: Attempted to make null choice");
                return;
            }
            
            if (_currentNode == null)
            {
                Debug.LogError("DialogueManager: No current dialogue node when making choice");
                return;
            }
            
            // Check if choice is available
            int alphaScore = _statsManager?.AlphaScore ?? 0;
            int betaScore = _statsManager?.BetaScore ?? 0;
            
            if (!choice.IsAvailable(alphaScore, betaScore))
            {
                Debug.LogWarning($"Choice '{choice.choiceID}' is not available with current stats");
                return;
            }
            
            // Apply personality impact
            if (choice.personalityImpact.HasImpact && _statsManager != null)
            {
                _statsManager.ApplyPersonalityImpact(choice.personalityImpact);
            }
            
            // Log telemetry
            _telemetryManager?.LogChoiceMade(
                _currentSceneID,
                choice.choiceID,
                choice.personalityImpact.alphaPointChange,
                choice.personalityImpact.betaPointChange
            );
            
            Debug.Log($"Choice Made: {choice.choiceID} | Impact: {choice.personalityImpact}");
            
            // Continue to next node
            if (choice.nextDialogueNode != null)
            {
                ProcessNode(choice.nextDialogueNode);
            }
            else
            {
                EndDialogue();
            }
        }
        
        public void ContinueDialogue()
        {
            if (_currentNode == null) return;
            
            if (_currentNode.isAutomatic && _currentNode.nextNode != null)
            {
                ProcessNode(_currentNode.nextNode);
            }
            else if (!_currentNode.HasChoices)
            {
                EndDialogue();
            }
        }
        
        private void ProcessNode(DialogueNode node)
        {
            _currentNode = node;
            
            Debug.Log($"Processing Dialogue Node: {node.nodeID} | Speaker: {node.speakerName}");
            
            // Notify UI of node change
            OnDialogueNodeChanged?.Invoke(node);
            
            // Handle special actions
            if (node.triggerPhoneNotification)
            {
                OnPhoneNotificationTriggered?.Invoke();
            }
            
            if (node.startFreeRoam)
            {
                OnFreeRoamStarted?.Invoke();
                return; // Don't process further, wait for free roam to complete
            }
            
            if (node.startMinigame)
            {
                OnMinigameStarted?.Invoke();
                return; // Don't process further, wait for minigame to complete
            }
            
            // Handle automatic progression
            if (node.isAutomatic && node.nextNode != null)
            {
                // Small delay for automatic progression to feel natural
                Invoke(nameof(ContinueDialogue), 2f);
            }
            // Handle choice presentation
            else if (node.HasChoices)
            {
                var availableChoices = GetAvailableChoices(node.choices);
                OnChoicesPresented?.Invoke(availableChoices);
            }
            // Handle end of dialogue
            else if (node.IsEndNode)
            {
                Invoke(nameof(EndDialogue), 2f);
            }
        }
        
        private List<ChoiceData> GetAvailableChoices(List<ChoiceData> allChoices)
        {
            var availableChoices = new List<ChoiceData>();
            int alphaScore = _statsManager?.AlphaScore ?? 0;
            int betaScore = _statsManager?.BetaScore ?? 0;
            
            foreach (var choice in allChoices)
            {
                if (choice != null && choice.IsAvailable(alphaScore, betaScore))
                {
                    availableChoices.Add(choice);
                }
            }
            
            return availableChoices;
        }
        
        private void EndDialogue()
        {
            Debug.Log("Dialogue Ended");
            _currentNode = null;
            OnDialogueEnded?.Invoke();
        }
        
        public void SetCurrentScene(string sceneID)
        {
            _currentSceneID = sceneID;
        }
        
        // For external systems to resume dialogue after special actions
        public void ResumeDialogue(DialogueNode nextNode = null)
        {
            if (nextNode != null)
            {
                ProcessNode(nextNode);
            }
            else if (_currentNode?.nextNode != null)
            {
                ProcessNode(_currentNode.nextNode);
            }
            else
            {
                EndDialogue();
            }
        }
        
        // Debug utilities
        [ContextMenu("Start Introduction")]
        private void Debug_StartIntroduction()
        {
            StartDialogue("DormRoom");
        }
        
        [ContextMenu("Start Campus Quad")]
        private void Debug_StartCampusQuad()
        {
            StartDialogue("CampusQuad");
        }
    }
}