using System;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes {
    public class VoiceClassNode : INode {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Voice_Class_Node";

        private VisualElement settingField;

        public AudioClip Clip;
        public float Volume = 1f;
        public float Pitch = 1f;
        public int Priority = 128;
        public bool Sync;
        public AudioMixerGroup AudioMixerGroup;

        protected override void OnOutputInit() {
            AddOutput("[Class]");
        }

        public override void Init(IEGraphView ieGraphView) {
            base.Init(ieGraphView);

            settingField = new VisualElement();

            mainContainer.AddClasses("uis-sound-class-brown-node");
            extensionContainer.AddClasses("sound-class-extension");
        }

        public override void Draw() {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Voice Class");
            titleLabel.AddClasses("ide-node__label");

            #region OBJECTFIELD

            var objectField = IEGraphUtility.CreateObjectField(typeof(AudioClip));
            objectField.value = Clip == null ? null : Clip;
            objectField.allowSceneObjects = false;
            objectField.AddClasses("ide-node__object-field-audio");

            objectField.RegisterValueChangedCallback((obj) => {
                Clip = (AudioClip)obj.newValue;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            var playBtn = IEGraphUtility.CreateButton("P", () => { NullUtils.PlayClip(Clip); });

            playBtn.AddClasses("uis-play-btn");

            var stopBtn = IEGraphUtility.CreateButton("S", NullUtils.StopAllClips);

            stopBtn.AddClasses("uis-stop-btn");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, objectField);
            titleContainer.Insert(2, playBtn);
            titleContainer.Insert(3, stopBtn);

            var volumeField = new VisualElement().LeftToRight();
            volumeField.AddClasses("ide-node__slider");

            var volumeLabel = IEGraphUtility.CreateLabel("Volume");
            volumeLabel.AddClasses("ide-node__label-slider");

            var volumeText = IEGraphUtility.CreateFloatField(Volume);
            var volumeSlider = IEGraphUtility.CreateSlider(Volume, 0f, 1f, x => {
                Volume = x.newValue;
                volumeText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });

            volumeSlider.RegisterCallback<FocusOutEvent>(_ => { GraphSaveUtility.SaveCurrent(); });

            volumeSlider.AddClasses("ide-node__slider");

            volumeText.RegisterCallback<FocusOutEvent>(_ => {
                if (Math.Abs(volumeSlider.value - volumeText.value) < double.Epsilon) return;
                var volume = volumeText.value;
                if (volume > 1) volume = 1;

                if (volume < 0) volume = 0;
                volumeSlider.value = volume;
                Volume = volume;
                volumeText.SetValueWithoutNotify(volume);
                GraphSaveUtility.SaveCurrent();
            });

            volumeText.AddClasses("ide-node__sound-float-field");

            volumeField.Add(volumeLabel);
            volumeField.Add(volumeSlider);
            volumeField.Add(volumeText);

            var pitchField = new VisualElement().LeftToRight();

            var pitchLabel = IEGraphUtility.CreateLabel("Pitch");
            pitchLabel.AddClasses("ide-node__label-slider");

            var pitchText = IEGraphUtility.CreateFloatField(Pitch);
            var pitchSlider = IEGraphUtility.CreateSlider(Pitch, -3f, 3f, x => {
                Pitch = x.newValue;
                pitchText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });

            pitchSlider.RegisterCallback<FocusOutEvent>(_ => { GraphSaveUtility.SaveCurrent(); });

            pitchSlider.AddClasses("ide-node__slider");

            pitchText.RegisterCallback<FocusOutEvent>(_ => {
                if (Math.Abs(pitchSlider.value - pitchText.value) < double.Epsilon) return;
                var pitch = pitchText.value;
                if (pitch > 3) pitch = 3;

                if (pitch < -3) pitch = -3;
                pitchSlider.value = pitch;
                Pitch = pitch;
                pitchText.SetValueWithoutNotify(pitch);
                GraphSaveUtility.SaveCurrent();
            });

            pitchText.AddClasses("ide-node__sound-float-field");

            pitchField.Add(pitchLabel);
            pitchField.Add(pitchSlider);
            pitchField.Add(pitchText);

            var audioMixerField = new VisualElement().LeftToRight();

            audioMixerField.tooltip = "If this option is active, it waits until the sound is completed.";

            var mixerLabel = IEGraphUtility.CreateLabel("Output");
            mixerLabel.AddClasses("ide-node__label-slider");
            
            var audioMixer = IEGraphUtility.CreateObjectField(typeof(AudioMixerGroup));
            audioMixer.value = AudioMixerGroup;
            audioMixer.RegisterValueChangedCallback(_ => {
                if (audioMixer.value == null) return;
                SetDirty();
                AudioMixerGroup = (AudioMixerGroup)audioMixer.value;
                GraphSaveUtility.SaveCurrent();
            });
            
            audioMixerField.Add(mixerLabel);
            audioMixerField.Add(audioMixer);

            var syncField = new VisualElement().LeftToRight();

            syncField.tooltip = "If active, the Typewriter effect will synchronize with the voice audio file.";

            var syncLabel = IEGraphUtility.CreateLabel("Sync");
            syncLabel.AddClasses("ide-node__label-slider");

            var syncToggle = IEGraphUtility.CreateToggle(null);
            syncToggle.value = Sync;
            syncToggle.RegisterCallback<ChangeEvent<bool>>(evt => {
                Sync = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            syncField.Add(syncLabel);
            syncField.Add(syncToggle);

            var priorityField = new VisualElement().LeftToRight();

            var priorityLabel = IEGraphUtility.CreateLabel("Priority");
            priorityLabel.AddClasses("ide-node__label-slider");

            var priorityText = IEGraphUtility.CreateIntField(Priority);
            var prioritySlider = IEGraphUtility.CreateSlider(Priority, 0, 256, x => {
                Priority = x.newValue;
                priorityText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });

            prioritySlider.RegisterCallback<FocusOutEvent>(_ => { GraphSaveUtility.SaveCurrent(); });

            prioritySlider.AddClasses("ide-node__slider");

            priorityText.RegisterCallback<FocusOutEvent>(_ => {
                if (prioritySlider.value == priorityText.value) return;
                var priority = priorityText.value;
                if (priority > 256) priority = 256;

                if (priority < 0) priority = 0;
                prioritySlider.value = priority;
                Priority = priority;
                priorityText.SetValueWithoutNotify(priority);

                GraphSaveUtility.SaveCurrent();
            });

            priorityText.AddClasses("ide-node__sound-int-field");

            priorityField.Add(priorityLabel);
            priorityField.Add(prioritySlider);
            priorityField.Add(priorityText);

            settingField.Insert(0, volumeField);
            settingField.Insert(1, pitchField);
            settingField.Insert(2, priorityField);
            settingField.Insert(3, audioMixerField);
            settingField.Insert(4, syncField);

            extensionContainer.Insert(0, settingField);

            foreach (var outputData in Outputs) {
                var cPort = this.CreatePort(outputData.Name, typeof(bool), Orientation.Vertical, Direction.Output, Port.Capacity.Multi);
                cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
                cPort.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
                cPort.portColor = STATIC.ClassPort;
                cPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();

            var element = topContainer.parent.Children().ElementAtOrDefault(2);
            element?.PlaceBehind(topContainer);
        }
    }
}