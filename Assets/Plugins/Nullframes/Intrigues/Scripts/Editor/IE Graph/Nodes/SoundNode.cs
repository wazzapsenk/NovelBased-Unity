using System;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SoundNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Play_Sound_Node";

        private VisualElement settingField;
        
        public AudioClip Clip;
        public float Volume = 1f;
        public float Pitch = 1f;
        public int Priority = 128;
        public bool WaitEnd;
        public AudioMixerGroup AudioMixerGroup;
        public bool Loop;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            settingField = new VisualElement();
            
            mainContainer.AddClasses("uis-brown-node");
            extensionContainer.AddClasses("sound-extension");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Play Sound");
            titleLabel.AddClasses("ide-node__label");

            #region OBJECTFIELD

            var objectField = IEGraphUtility.CreateObjectField(typeof(AudioClip));
            objectField.value = Clip == null ? null : Clip;
            objectField.allowSceneObjects = false;
            objectField.AddClasses("ide-node__object-field-audio");

            objectField.RegisterValueChangedCallback((obj) =>
            {
                Clip = (AudioClip)obj.newValue;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            var playBtn = IEGraphUtility.CreateButton("P", () =>
            {
                NullUtils.PlayClip(Clip);
            });
            
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
            var volumeSlider = IEGraphUtility.CreateSlider(Volume, 0f, 1f, x =>
            {
                Volume = x.newValue;
                volumeText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });
            
            volumeSlider.RegisterCallback<FocusOutEvent>(_ =>
            {
                GraphSaveUtility.SaveCurrent();
            });

            volumeSlider.AddClasses("ide-node__slider");

            volumeText.RegisterCallback<FocusOutEvent>(_ =>
            {
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
            var pitchSlider = IEGraphUtility.CreateSlider(Pitch, -3f, 3f, x =>
            {
                Pitch = x.newValue;
                pitchText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });
            
            pitchSlider.RegisterCallback<FocusOutEvent>(_ =>
            {
                GraphSaveUtility.SaveCurrent();
            });

            pitchSlider.AddClasses("ide-node__slider");

            pitchText.RegisterCallback<FocusOutEvent>(_ =>
            {
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
                AudioMixerGroup = (AudioMixerGroup)audioMixer.value;
                GraphSaveUtility.SaveCurrent();
            });
            
            audioMixerField.Add(mixerLabel);
            audioMixerField.Add(audioMixer);
            
            var delayField = new VisualElement().LeftToRight();

            delayField.tooltip = "If this option is active, it waits until the sound is completed.";

            var delayLabel = IEGraphUtility.CreateLabel("Wait End");
            delayLabel.AddClasses("ide-node__label-slider");
            
            var delayToggle = IEGraphUtility.CreateToggle(null);
            delayToggle.value = WaitEnd;
            delayToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                WaitEnd = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });
            
            delayField.Add(delayLabel);
            delayField.Add(delayToggle);
            
            var loopField = new VisualElement().LeftToRight();

            var loopLabel = IEGraphUtility.CreateLabel("Loop(\u221E)");
            loopLabel.AddClasses("ide-node__label-slider");

            var loopToggle = IEGraphUtility.CreateToggle(null);
            loopToggle.value = Loop;
            loopToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                Loop = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            loopField.Add(loopLabel);
            loopField.Add(loopToggle);

            var priorityField = new VisualElement().LeftToRight();

            var priorityLabel = IEGraphUtility.CreateLabel("Priority");
            priorityLabel.AddClasses("ide-node__label-slider");

            var priorityText = IEGraphUtility.CreateIntField(Priority);
            var prioritySlider = IEGraphUtility.CreateSlider(Priority, 0, 256, x =>
            {
                Priority = x.newValue;
                priorityText.SetValueWithoutNotify(x.newValue);
                SetDirty();
            });
            
            prioritySlider.RegisterCallback<FocusOutEvent>(_ =>
            {
                GraphSaveUtility.SaveCurrent();
            });

            prioritySlider.AddClasses("ide-node__slider");

            priorityText.RegisterCallback<FocusOutEvent>(_ =>
            {
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
            // settingField.Insert(3, loopField);
            settingField.Insert(3, audioMixerField);
            settingField.Insert(4, delayField);

            extensionContainer.Insert(0, settingField);

            var global =
                this.CreatePort("Global", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(global);
            
            var conspirator =
                this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(conspirator);
            
            var target =
                this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(target);

            var actor = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actor.portColor = STATIC.GreenPort;
            actor.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(actor);
            
            foreach (var schemeVariable in ((SchemeGroup)Group).Variables.Where(v => v.type == NType.Actor)) {
                var port = this.CreatePort($"[{schemeVariable.name}]", typeof(bool), Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi);
                port.userData = schemeVariable.id;
                port.portColor = STATIC.BluePort;
                port.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                inputContainer.Add(port);
            }

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }
            
            RefreshExpandedState();
        }
    }
}