using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CollegeChronicles.Data
{
    [CreateAssetMenu(fileName = "Dialogue Database", menuName = "College Chronicles/Dialogue Database")]
    public class DialogueDatabase : ScriptableObject
    {
        [Header("Characters")]
        public List<CharacterData> characters = new List<CharacterData>();
        
        [Header("Dialogue Nodes")]
        public List<DialogueNode> dialogueNodes = new List<DialogueNode>();
        
        [Header("Choices")]
        public List<ChoiceData> choices = new List<ChoiceData>();
        
        [Header("Starting Points")]
        public DialogueNode introductionNode;
        public DialogueNode campusQuadNode;
        
        private Dictionary<string, DialogueNode> _nodeDict;
        private Dictionary<string, ChoiceData> _choiceDict;
        private Dictionary<string, CharacterData> _characterDict;
        
        private void OnEnable()
        {
            BuildDictionaries();
        }
        
        private void BuildDictionaries()
        {
            // Build node dictionary
            _nodeDict = new Dictionary<string, DialogueNode>();
            foreach (var node in dialogueNodes.Where(n => n != null))
            {
                if (!string.IsNullOrEmpty(node.nodeID))
                {
                    _nodeDict[node.nodeID] = node;
                }
            }
            
            // Build choice dictionary
            _choiceDict = new Dictionary<string, ChoiceData>();
            foreach (var choice in choices.Where(c => c != null))
            {
                if (!string.IsNullOrEmpty(choice.choiceID))
                {
                    _choiceDict[choice.choiceID] = choice;
                }
            }
            
            // Build character dictionary
            _characterDict = new Dictionary<string, CharacterData>();
            foreach (var character in characters.Where(c => c != null))
            {
                if (!string.IsNullOrEmpty(character.characterID))
                {
                    _characterDict[character.characterID] = character;
                }
            }
        }
        
        public DialogueNode GetNode(string nodeID)
        {
            if (_nodeDict == null) BuildDictionaries();
            return _nodeDict.TryGetValue(nodeID, out DialogueNode node) ? node : null;
        }
        
        public ChoiceData GetChoice(string choiceID)
        {
            if (_choiceDict == null) BuildDictionaries();
            return _choiceDict.TryGetValue(choiceID, out ChoiceData choice) ? choice : null;
        }
        
        public CharacterData GetCharacter(string characterID)
        {
            if (_characterDict == null) BuildDictionaries();
            return _characterDict.TryGetValue(characterID, out CharacterData character) ? character : null;
        }
        
        public DialogueNode GetStartingNode(string sceneID)
        {
            switch (sceneID.ToLower())
            {
                case "dormroom":
                case "introduction":
                    return introductionNode;
                case "campusquad":
                case "quad":
                    return campusQuadNode;
                default:
                    return introductionNode;
            }
        }
        
        private void OnValidate()
        {
            // Ensure all references are valid
            dialogueNodes.RemoveAll(node => node == null);
            choices.RemoveAll(choice => choice == null);
            characters.RemoveAll(character => character == null);
        }
    }
}