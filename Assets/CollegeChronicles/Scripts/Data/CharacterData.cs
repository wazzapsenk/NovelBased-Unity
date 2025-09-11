using UnityEngine;
using System.Collections.Generic;

namespace CollegeChronicles.Data
{
    [CreateAssetMenu(fileName = "New Character", menuName = "College Chronicles/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Basic Information")]
        public string characterName;
        public string characterID;
        
        [Header("Visual Data")]
        public Sprite neutralExpression;
        public Sprite happyExpression;
        public Sprite angryExpression;
        
        [Header("Character Description")]
        [TextArea(3, 6)]
        public string characterDescription;
        
        [Header("Voice and Personality")]
        [TextArea(2, 4)]
        public string voiceGuidelines;
        
        private Dictionary<string, Sprite> _expressionDict;
        
        private void OnEnable()
        {
            BuildExpressionDictionary();
        }
        
        private void BuildExpressionDictionary()
        {
            _expressionDict = new Dictionary<string, Sprite>
            {
                { "Neutral", neutralExpression },
                { "Happy", happyExpression },
                { "Angry", angryExpression }
            };
        }
        
        public Sprite GetExpression(string expressionName)
        {
            if (_expressionDict == null)
                BuildExpressionDictionary();
                
            return _expressionDict.TryGetValue(expressionName, out Sprite expression) 
                ? expression 
                : neutralExpression;
        }
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(characterID))
            {
                characterID = name;
            }
        }
    }
}