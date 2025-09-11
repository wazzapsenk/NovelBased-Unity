using UnityEngine;
using System.Collections.Generic;

namespace CollegeChronicles.Data
{
    [CreateAssetMenu(fileName = "New Dialogue Node", menuName = "College Chronicles/Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        [Header("Basic Information")]
        public string nodeID;
        public string speakerName;
        
        [Header("Dialogue Content")]
        [TextArea(3, 6)]
        public string dialogueText;
        
        [Header("Character Expression")]
        public string characterExpression = "Neutral";
        
        [Header("Choices")]
        public List<ChoiceData> choices = new List<ChoiceData>();
        
        [Header("Auto-Continue")]
        public bool isAutomatic = false;
        public DialogueNode nextNode;
        
        [Header("Scene Actions")]
        public bool triggerPhoneNotification = false;
        public bool startFreeRoam = false;
        public bool startMinigame = false;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(nodeID))
            {
                nodeID = name;
            }
        }
        
        public bool HasChoices => choices != null && choices.Count > 0;
        public bool IsEndNode => !isAutomatic && !HasChoices && nextNode == null;
    }
}