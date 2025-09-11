using UnityEngine;

namespace CollegeChronicles.Data
{
    [CreateAssetMenu(fileName = "New Choice", menuName = "College Chronicles/Choice Data")]
    public class ChoiceData : ScriptableObject
    {
        [Header("Choice Information")]
        public string choiceID;
        
        [Header("Display")]
        [TextArea(2, 4)]
        public string choiceText;
        
        [Header("Personality Impact")]
        public PersonalityImpact personalityImpact;
        
        [Header("Navigation")]
        public DialogueNode nextDialogueNode;
        
        [Header("Requirements")]
        public int requiredAlphaScore = 0;
        public int requiredBetaScore = 0;
        public bool isAlwaysAvailable = true;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(choiceID))
            {
                choiceID = name;
            }
        }
        
        public bool IsAvailable(int currentAlpha, int currentBeta)
        {
            if (isAlwaysAvailable) return true;
            
            return currentAlpha >= requiredAlphaScore && currentBeta >= requiredBetaScore;
        }
        
        public string GetDisplayText()
        {
            return choiceText;
        }
    }
}