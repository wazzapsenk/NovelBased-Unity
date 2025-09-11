using System;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Nullframes.Intrigues.UI {
    public class DialogueManager : IIEditor {
        private static DialogueManager instance;

        public static DialogueManager Instance {
            get {
                if (instance != null) return instance;
                _ = IM.DialogueManager ??
                    throw new ArgumentException(STATIC.DEBUG_DIALOGUE_MANAGER_NOT_FOUND);
                var dialogueManager = Instantiate(IM.DialogueManager.gameObject, IM.Transform)
                    .GetComponent<DialogueManager>();
                instance = dialogueManager;
                return instance;
            }
        }

        private void Awake() {
            SceneManager.activeSceneChanged += (_, _) => {
                // if (CloseWhenActiveSceneChanged) {
                    foreach (var panel in Instance.GetComponentsInChildren<DialoguePanel>(true)) {
                        // if (panel.Dialogue is { IsSchemeDialog: true }) continue;
                        panel.Dialogue?.Close(true);
                    }
                // }
            };
        }

        /// <summary>
        /// Determines whether the most recently opened dialogue should be displayed on top.
        /// </summary>
        /// <remarks>
        /// When set to true, this property ensures that the latest opened dialogue is always displayed at the top of the stack, 
        /// making it the most visible or prioritized dialogue. This is useful in scenarios where multiple dialogues are opened 
        /// and the most recent one needs immediate attention or interaction.
        /// </remarks>
        public bool LastOnTop;
        // public bool CloseWhenActiveSceneChanged;
        
        public bool unscaledTime;

        public Color conditionalChoiceColor = new (0.4980392156862745f, 0.6235294117647059f, 0.8588235294117647f);
        public Color disabledChoiceColor = new (0.8196078431372549f, 0.35294117647058826f, 0.2823529411764706f);
        public Color CircleKeywordColor = new (0.8196078431372549f, 0.8392156862745098f, 0.47058823529411764f);
        public Color SquareKeywordColor = new (0.8392156862745098f, 0.5568627450980392f, 0.3843137254901961f);
        public Color LinkColor = new (0.5568627450980392f, 0.8117647058823529f, 0.4235294117647059f);

        public DialoguePanel dialoguePanel;

        [Space(10)] 
        public UnityEvent<Dialogue> onDialogueOpen; // Event triggered when a dialogue is opened.
        public UnityEvent<Dialogue> onDialogueClose; // Event triggered when a dialogue is closed.
        public UnityEvent<Dialogue> onDialogueShow; // Event triggered when a dialogue is shows.
        public UnityEvent<Dialogue> onDialogueHide; // Event triggered when a dialogue is hides.
        
        public Action<Dialogue> onDialogueClose_Ignore;

        /// <summary>
        /// Opens a dialogue with the specified title and content.
        /// </summary>
        /// <param name="title">The title of the dialogue.</param>
        /// <param name="content">The content of the dialogue.</param>
        /// <param name="time">The duration of the dialog window. The window closes when the time is up.</param>
        /// <param name="isSchemeDialog">This indicates whether the dialog is included in a node flow or not.</param>
        /// <param name="typeWriter">If true, the Typewriter effect is applied.</param>
        /// <param name="onUpdate">.</param>
        /// <returns>The opened dialogue.</returns>
        public static Dialogue OpenDialogue(string title, string content, float time = 0, bool typeWriter = false, Action<float> onUpdate = null,
            bool isSchemeDialog = false) {
            // Create a new instance of the dialogue panel by duplicating the original dialogue panel.
            var _dialoguePanel = Instance.dialoguePanel.gameObject.Duplicate<DialoguePanel>();

            content = content.Replace("<link=\"",
                $"<b><color=#{ColorUtility.ToHtmlStringRGB(Instance.LinkColor)}><link=\"");
            
            // If the "FirstInFirstOut" flag is enabled, set the sibling index of the duplicated dialogue panel to 1,
            // so that it appears above other dialogues in the hierarchy.
            if (!Instance.LastOnTop)
                _dialoguePanel.transform.SetSiblingIndex(1);

            content = content.Replace("</link>", "</link></color></b>");

            // Set up the duplicated dialogue panel with the specified title and content.
            _dialoguePanel.Setup(title, content);
            
            // Create a new dialogue instance using the duplicated dialogue panel.
            var dialogue = new Dialogue(_dialoguePanel, isSchemeDialog);
            
            dialogue.Init(time, typeWriter, onUpdate);

            // Activate the game object of the duplicated dialogue panel.
            _dialoguePanel.gameObject.SetActive(true);

            // Trigger the onDialogueOpen event of the DialogueManager instance.
            Instance.onDialogueOpen.Invoke(dialogue);

            _dialoguePanel.Dialogue = dialogue;
            

            // Return the opened dialogue.
            return dialogue;
        }

        /// <summary>
        /// Closes the dialogue windows.
        /// </summary>
        /// <param name="justSchemeDialog">If true, only scheme dialogue windows are closed. If false, all dialogue windows are closed.</param>
        /// <param name="withoutNotification">If true, closes the dialogues without sending notifications.</param>
        /// <remarks>
        /// This method iterates through all DialoguePanel instances and closes them. If 'justSchemeDialog' is true, 
        /// it only closes those that are marked as scheme dialogs. The 'withoutNotification' parameter determines 
        /// whether to close the dialogues silently without triggering additional notifications.
        /// </remarks>
        public static void Close(bool justSchemeDialog = false, bool withoutNotification = false) {
            if (Instance == null) return;
            foreach (var panel in Instance.GetComponentsInChildren<DialoguePanel>(true)) {
                if (justSchemeDialog && panel.Dialogue is { IsSchemeDialog: false }) continue;
                panel.Dialogue?.Close(withoutNotification);
            }
        }

        /// <summary>
        /// Shows hidden dialogue windows.
        /// </summary>
        /// <param name="justSchemeDialog">If true, only shows scheme dialogue windows. If false, shows all dialogue windows.</param>
        /// <param name="withoutNotification">If true, hides the dialogues without sending notifications.</param>
        /// <remarks>
        /// This method goes through all DialoguePanel instances and makes them visible. If 'justSchemeDialog' is true, 
        /// only the dialogue windows marked as scheme dialogs are made visible.
        /// </remarks>
        public static void HideAll(bool justSchemeDialog = false, bool withoutNotification = false) {
            if (Instance == null) return;
            foreach (var panel in Instance.GetComponentsInChildren<DialoguePanel>(true)) {
                if (justSchemeDialog && panel.Dialogue is { IsSchemeDialog: false }) continue;
                panel.Dialogue?.Hide(withoutNotification);
            }
        }

        /// <summary>
        /// Hides the dialogue windows.
        /// </summary>
        /// <param name="justSchemeDialog">If true, only hides scheme dialogue windows. If false, hides all dialogue windows.</param>
        /// <param name="withoutNotification">If true, shows the dialogues without sending notifications.</param>
        /// <remarks>
        /// This method iterates through each DialoguePanel instance and hides them. If 'justSchemeDialog' is set to true, 
        /// it only hides those dialogues that are identified as scheme dialogs.
        /// </remarks>
        public static void ShowAll(bool justSchemeDialog = false, bool withoutNotification = false) {
            if (Instance == null) return;
            foreach (var panel in Instance.GetComponentsInChildren<DialoguePanel>(true)) {
                if (justSchemeDialog && panel.Dialogue is { IsSchemeDialog: false }) continue;
                panel.Dialogue?.Show(withoutNotification);
            }
        }
    }
}