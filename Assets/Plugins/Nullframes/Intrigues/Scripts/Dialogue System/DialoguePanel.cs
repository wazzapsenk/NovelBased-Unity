using System;
using Nullframes.Intrigues.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI
{
    public class DialoguePanel : IIEditor
    {
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI contentLabel;
        public TextMeshProUGUI counterLabel;
        public Image background;
        public TypewriterEffect typewriter;
        
        public Choice choiceLayout;
        
        public Dialogue Dialogue;

        [HideInInspector] public string counterId;
        
        public UnityEvent hasTimer;
        
        private void Awake()
        {
            if (transform.GetSiblingIndex() == 0) gameObject.SetActive(false);

            choiceLayout.gameObject.SetActive(false);
        }

        public void Setup(string title, string content)
        {
            titleLabel.text = title;
            contentLabel.text = content;
            
            contentLabel.text = contentLabel.text.Replace("\u25cf", $"<size=42><color=#{ColorUtility.ToHtmlStringRGB(DialogueManager.Instance.CircleKeywordColor)}>\u25cf</color></size>");
            contentLabel.text = contentLabel.text.Replace("\u25A0", $"<size=32><color=#{ColorUtility.ToHtmlStringRGB(DialogueManager.Instance.SquareKeywordColor)}>\u25A0</color></size>");
        }

        public Button AddButton(string text, Sprite icon, Action action, Func<bool> condition = null, bool closeWhenSelected = true, bool hideIfConditionNotMet = false, ChoiceData data = null) {
            var duplicatedChoice = choiceLayout.gameObject.Duplicate<Choice>();
            var button = duplicatedChoice.button;
            var tmPro = duplicatedChoice.text;
            var image = duplicatedChoice.icon;
            
            _ = tmPro ?? throw new Exception(STATIC.CHOICE_TEXT_MESH_NOT_FOUND);

            tmPro.text = text;
            image.sprite = icon;
            duplicatedChoice.gameObject.SetActive(true);
            
            var choiceList = duplicatedChoice.transform.GetComponents<IChoiceData>();
            
            foreach (var choiceData in choiceList) {
                choiceData.Init(data);
            }
            
            if (condition != null)
            {
                if (!condition.Invoke()) {
                    foreach (var choiceData in choiceList) {
                        choiceData.OnDisabled();
                    }
                    button.interactable = false;
                    tmPro.color = DialogueManager.Instance.disabledChoiceColor;
                }
                else {
                    foreach (var choiceData in choiceList) {
                        choiceData.OnEnabled();
                    }
                    tmPro.color = DialogueManager.Instance.conditionalChoiceColor;
                }
            }
            
            if (hideIfConditionNotMet)
            {
                if (condition?.Invoke() == false)
                {
                    duplicatedChoice.gameObject.SetActive(false);
                }
            }
            
            var id = NullUtils.DelayedCall(new DelayedCallParams {
                Delay = 0.2f,
                Call = Condition,
                UnscaledTime = true,
                LoopCount = -1,
            });
            
            DialogueManager.Instance.onDialogueClose_Ignore += (d => {
                if (d == Dialogue) {
                    NullUtils.StopCall(id);
                }
            });

            action += () =>
            {
                if (!closeWhenSelected) return;
                transform.SetParent(null);
                Destroy(gameObject);
                DialogueManager.Instance.onDialogueClose.Invoke(Dialogue);
                Dialogue.onDialogueClose?.Invoke();
                DialogueManager.Instance.onDialogueClose_Ignore?.Invoke(Dialogue);
            };

            void Condition() {
                if (condition != null) {
                    if (!condition.Invoke()) {
                        if (button.interactable) {
                            foreach (var choiceData in choiceList) {
                                choiceData.OnDisabled();
                            }
                        }
                        button.interactable = false;
                        tmPro.color = DialogueManager.Instance.disabledChoiceColor;
                    } else {
                        if (!button.interactable) {
                            foreach (var choiceData in choiceList) {
                                choiceData.OnEnabled();
                            }
                        }
                        button.interactable = true;
                        tmPro.color = DialogueManager.Instance.conditionalChoiceColor;
                    }
                }
            }

            button.onClick.AddListener(() => {
                if (condition != null && !condition.Invoke()) return;
                
                action.Invoke();
                NullUtils.StopCall(counterId);
            });

            if (image.sprite == null) image.gameObject.SetActive(false);

            return button;
        }

        private void Update() {
            Dialogue?.Update();
        }
    }
}