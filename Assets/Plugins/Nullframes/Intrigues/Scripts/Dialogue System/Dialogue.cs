using System;
using System.Collections.Generic;
using Nullframes.Intrigues.Graph;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues.UI
{
    /// <summary>
    /// Represents a dialogue with various functionalities including adding choices and managing visibility.
    /// </summary>
    /// <remarks>
    /// The Dialogue class encapsulates the behavior and properties of a dialogue, including the ability to show, hide, and close the dialogue, 
    /// as well as to add choices to it. It is associated with a DialoguePanel which is used to display the dialogue in the user interface.
    /// </remarks>
    public class Dialogue
    {
        private readonly DialoguePanel dialoguePanel;

        public bool IsSchemeDialog { get; }
        
        public Action onTimeout;
        
        public Action onDialogueClose; // Event triggered when a dialogue is closed.
        public Action onDialogueShow; // Event triggered when a dialogue is shows.
        public Action onDialogueHide; // Event triggered when a dialogue is hides.

        public bool ShowedUp { get; private set; }

        private Dictionary<SoundClassData, AudioSource> soundSources = new();
        private Dictionary<VoiceClassData, AudioSource> voiceSources = new();

        public Dialogue(DialoguePanel panel, bool isSchemeDialog)
        {
            dialoguePanel = panel;
            firstParent = dialoguePanel.transform.parent;
            IsSchemeDialog = isSchemeDialog;
        }

        /// <summary>
        /// Closes the dialogue and optionally performs additional actions.
        /// </summary>
        /// <param name="withoutNotification">If set to true, closes the dialogue without triggering additional actions like playing sound or invoking close event.</param>
        /// <remarks>
        /// This method destroys the dialogue's associated panel. If 'withoutNotification' is false, it also triggers the onDialogueClose event 
        /// and plays a closing sound effect.
        /// </remarks>
        public void Close(bool withoutNotification = false)
        {
            if (dialoguePanel == null) return;

            NullUtils.StopCall(dialoguePanel.counterId);
            
            Object.Destroy(dialoguePanel.gameObject);
            if (!withoutNotification) {
                DialogueManager.Instance.onDialogueClose.Invoke(this);
            }
            onDialogueClose?.Invoke();
            DialogueManager.Instance.onDialogueClose_Ignore?.Invoke(this);
        }

        private Transform firstParent;

        /// <summary>
        /// Hides the dialogue.
        /// </summary>
        /// <remarks>
        /// This method sets the dialogue's associated panel as inactive and pauses any ongoing timers.
        /// A closing sound effect is played when the dialogue is hidden.
        /// </remarks>
        public void Hide(bool withoutNotification = false)
        {
            if (dialoguePanel == null) return;
            dialoguePanel.transform.SetParent(firstParent.parent);
            dialoguePanel.gameObject.SetActive(false);
            
            NullUtils.PauseCall(dialoguePanel.counterId);
            if (!withoutNotification) {
                DialogueManager.Instance.onDialogueHide.Invoke(this);
            }
            onDialogueHide?.Invoke();
        }
        
        /// <summary>
        /// Shows the dialogue.
        /// </summary>
        /// <remarks>
        /// This method sets the dialogue's associated panel as active and resumes any paused timers.
        /// A closing sound effect is played when the dialogue is shown.
        /// </remarks>
        public void Show(bool withoutNotification = false)
        {
            if (dialoguePanel == null) return;
            dialoguePanel.transform.SetParent(firstParent);
            dialoguePanel.gameObject.SetActive(true);

            NullUtils.ResumeCall(dialoguePanel.counterId);
            if (!withoutNotification) {
                DialogueManager.Instance.onDialogueShow.Invoke(this);
            }
            onDialogueShow?.Invoke();
        }

        /// <summary>
        /// Adds a choice to the dialogue with various configurations.
        /// </summary>
        /// <param name="text">The text of the choice.</param>
        /// <param name="icon">Optional icon for the choice.</param>
        /// <param name="action">Optional action to execute when the choice is selected.</param>
        /// <param name="condition">Optional condition to determine if the choice should be shown.</param>
        /// <param name="closeWhenSelected">Determines if the dialogue should close when the choice is selected.</param>
        /// <param name="hideIfConditionNotMet">Determines if the choice should be hidden if the condition is not met.</param>
        /// <param name="data">Additional data associated with the choice.</param>
        /// <returns>The current Dialogue instance for chaining.</returns>
        /// <remarks>
        /// This method allows adding a choice to the dialogue with various options like text, icon, action to execute, conditions for showing the choice,
        /// and whether the dialogue should close when the choice is selected.
        /// Overloads of this method provide simplified ways to add choices with fewer parameters.
        /// </remarks>
        public Dialogue AddChoice(string text, Sprite icon, Action action, Func<bool> condition = null,
            bool closeWhenSelected = true, bool hideIfConditionNotMet = false, ChoiceData data = null)
        {
            dialoguePanel.AddButton(text, icon, action, condition, closeWhenSelected, hideIfConditionNotMet, data);
            return this;
        }
        
        /// <summary>
        /// Adds a choice to the dialogue with various configurations.
        /// </summary>
        /// <param name="text">The text of the choice.</param>
        /// <returns>The current Dialogue instance for chaining.</returns>
        /// <remarks>
        /// This method allows adding a choice to the dialogue with various options like text, icon, action to execute, conditions for showing the choice,
        /// and whether the dialogue should close when the choice is selected.
        /// Overloads of this method provide simplified ways to add choices with fewer parameters.
        /// </remarks>
        public Dialogue AddChoice(string text)
        {
            dialoguePanel.AddButton(text, null, null);
            return this;
        }

        /// <summary>
        /// Adds a choice to the dialogue with various configurations.
        /// </summary>
        /// <param name="text">The text of the choice.</param>
        /// <param name="icon">Optional icon for the choice.</param>
        /// <returns>The current Dialogue instance for chaining.</returns>
        /// <remarks>
        /// This method allows adding a choice to the dialogue with various options like text, icon, action to execute, conditions for showing the choice,
        /// and whether the dialogue should close when the choice is selected.
        /// Overloads of this method provide simplified ways to add choices with fewer parameters.
        /// </remarks>
        public Dialogue AddChoice(string text, Sprite icon)
        {
            dialoguePanel.AddButton(text, icon, null);
            return this;
        }

        /// <summary>
        /// Adds a choice to the dialogue with various configurations.
        /// </summary>
        /// <param name="text">The text of the choice.</param>
        /// <param name="action">Optional action to execute when the choice is selected.</param>
        /// <returns>The current Dialogue instance for chaining.</returns>
        /// <remarks>
        /// This method allows adding a choice to the dialogue with various options like text, icon, action to execute, conditions for showing the choice,
        /// and whether the dialogue should close when the choice is selected.
        /// Overloads of this method provide simplified ways to add choices with fewer parameters.
        /// </remarks>
        public Dialogue AddChoice(string text, Action action)
        {
            dialoguePanel.AddButton(text, null, action);
            return this;
        }

        public Dialogue SetBackground(Sprite background) {
            if (dialoguePanel.background == null) return this;
            dialoguePanel.background.sprite = background;
            return this;
        }

        public Dialogue SetNativeSize() {
            if (dialoguePanel.background == null) return this;
            dialoguePanel.background.SetNativeSize();
            return this;
        }

        public Dialogue SetTypeWriterDuration(float seconds) {
            dialoguePanel.typewriter.seconds = seconds;
            return this;
        }

        public void Init(float time, bool typeWriter, Action<float> onUpdate) {
            NullUtils.DelayedCall(new DelayedCallParams {
                WaitUntil = () => dialoguePanel == null || dialoguePanel.transform.parent == null || dialoguePanel.transform.GetSiblingIndex() == dialoguePanel.transform.parent.childCount - 1,
                Call = () => {
                    if (dialoguePanel == null) {
                        return;
                    }

                    if (time > 0) {
                        dialoguePanel.counterId
                            = NullUtils.GenerateID();
                        NullUtils.DelayedCall(new DelayedCallParams {
                            DelayName = dialoguePanel.counterId,
                            Delay = time,
                            Call = () => {
                                onTimeout?.Invoke();
                                Close();
                            },
                            OnUpdate = f => {
                                dialoguePanel.counterLabel.text = Mathf.RoundToInt(f).ToString();
                                onUpdate?.Invoke(f);
                            },
                            UnscaledTime = DialogueManager.Instance.unscaledTime
                        });
                        // NullUtils.DelayedCall(dialoguePanel.counterId, null, time, () => {
                        //     onTimeout?.Invoke();
                        //     Close();
                        // }, f => {
                        //     dialoguePanel.counterLabel.text = Mathf.RoundToInt(f).ToString();
                        //     onUpdate?.Invoke(f);
                        // }, DialogueManager.Instance.unscaledTime);

                        dialoguePanel.hasTimer.Invoke();
                    }
            
                    //Start TypeWriter Effect
                    if (typeWriter) {
                        dialoguePanel.typewriter.StartTypeWriter();
                    }
                
                    foreach (var audioSource in soundSources) {
                        audioSource.Value.Play();
                    }
                
                    foreach (var audioSource in voiceSources) {
                        audioSource.Value.Play();
                    }

                    ShowedUp = true;
                }
            });
            // NullUtils.DelayedCall(null, () => dialoguePanel == null || dialoguePanel.transform.parent == null || dialoguePanel.transform.GetSiblingIndex() == dialoguePanel.transform.parent.childCount - 1, () => {
            //     if (dialoguePanel == null) {
            //         return;
            //     }
            //
            //     if (time > 0) {
            //         dialoguePanel.counterId
            //             = NullUtils.GenerateID();
            //         NullUtils.DelayedCall(dialoguePanel.counterId, null, time, () => {
            //             onTimeout?.Invoke();
            //             Close();
            //         }, f => {
            //             dialoguePanel.counterLabel.text = Mathf.RoundToInt(f).ToString();
            //             onUpdate?.Invoke(f);
            //         }, DialogueManager.Instance.unscaledTime);
            //
            //         dialoguePanel.hasTimer.Invoke();
            //     }
            //
            //     //Start TypeWriter Effect
            //     if (typeWriter) {
            //         dialoguePanel.typewriter.StartTypeWriter();
            //     }
            //     
            //     foreach (var audioSource in soundSources) {
            //         audioSource.Value.Play();
            //     }
            //     
            //     foreach (var audioSource in voiceSources) {
            //         audioSource.Value.Play();
            //     }
            //
            //     ShowedUp = true;
            // });
        }

        public (string, AudioSource) AddSound(SoundClassData soundClassData) {
            var soundId = NullUtils.GenerateID();

            var soundSource = IM.SetupAudio(soundId, soundClassData.Clip,
                soundClassData.Volume);
            if (soundSource == null) return (null, null);
            soundSource.pitch = soundClassData.Pitch;
            soundSource.volume = soundClassData.Volume;
            soundSource.outputAudioMixerGroup = soundClassData.AudioMixerGroup;
            soundSource.loop = soundClassData.Loop;
            
            soundSources.Add(soundClassData, soundSource);
            return (soundId, soundSource);
        }
        
        public (string, AudioSource) AddSound(VoiceClassData voiceClassData) {
            var soundId = NullUtils.GenerateID();

            var voiceSource = IM.SetupAudio(soundId, voiceClassData.Clip,
                voiceClassData.Volume);
            if (voiceSource == null) return (null, null);
            voiceSource.pitch = voiceClassData.Pitch;
            voiceSource.volume = voiceClassData.Volume;
            voiceSource.outputAudioMixerGroup = voiceClassData.AudioMixerGroup;

            voiceSources.Add(voiceClassData, voiceSource);
            return (soundId, voiceSource);
        }

        public void Update() {
            if (ShowedUp) {
                if (soundSources.Count > 0) {
                    if (IsTop) {
                        foreach (var audioSource in soundSources) {
                            if(audioSource.Value != null)
                                audioSource.Value.UnPause();
                        }
                    }
                    else {
                        foreach (var audioSource in soundSources) {
                            if(audioSource.Value != null)
                                audioSource.Value.Pause();
                        }
                    }
                }
                
                if (voiceSources.Count > 0) {
                    if (IsTop) {
                        foreach (var audioSource in voiceSources) {
                            if(audioSource.Value != null)
                                audioSource.Value.UnPause();
                        }
                    }
                    else {
                        foreach (var audioSource in voiceSources) {
                            if(audioSource.Value != null)
                                audioSource.Value.Pause();
                        }
                    }
                }
            }
        }

        public bool IsTop => dialoguePanel != null && dialoguePanel.transform.parent != null && dialoguePanel.transform.GetSiblingIndex() == dialoguePanel.transform.parent.childCount - 1;
        

        /// <summary>
        /// Adds a choice to the dialogue with various configurations.
        /// </summary>
        /// <param name="text">The text of the choice.</param>
        /// <param name="icon">Optional icon for the choice.</param>
        /// <param name="action">Optional action to execute when the choice is selected.</param>
        /// <param name="closeWhenSelected">Determines if the dialogue should close when the choice is selected.</param>
        /// <returns>The current Dialogue instance for chaining.</returns>
        /// <remarks>
        /// This method allows adding a choice to the dialogue with various options like text, icon, action to execute, conditions for showing the choice,
        /// and whether the dialogue should close when the choice is selected.
        /// Overloads of this method provide simplified ways to add choices with fewer parameters.
        /// </remarks>
        public Dialogue AddChoice(string text, Sprite icon, Action action, bool closeWhenSelected)
        {
            dialoguePanel.AddButton(text, icon, action, null, closeWhenSelected);
            return this;
        }
    }
}