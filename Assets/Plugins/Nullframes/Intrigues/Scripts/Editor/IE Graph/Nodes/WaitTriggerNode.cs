using System;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class WaitTriggerNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Wait_Trigger_Node";

        public string TriggerName;
        public float Timeout;
        
        protected override void OnOutputInit()
        {
            AddOutput("Trigger(True)");
            AddOutput("Trigger(False)");
            AddOutput("Timeout");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-byzantine-node");
            extensionContainer.AddClasses("wait-trigger-extension");
            inputContainer.style.justifyContent = new StyleEnum<Justify>(Justify.Center);

            mainContainer.style.minWidth = 300f;
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Wait Trigger");
            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            #region NAME

            var triggerField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var triggerLabel = IEGraphUtility.CreateLabel("Name");
            triggerLabel.style.fontSize = 12;
            triggerLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            triggerLabel.style.paddingRight = 10f;
            triggerLabel.style.minWidth = 90f;
            triggerLabel.style.maxWidth = 90f;

            //TextField
            var trigger = IEGraphUtility.CreateTextField(TriggerName);
            trigger.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (TriggerName == trigger.value) return;
                TriggerName = trigger.value;
                GraphSaveUtility.SaveCurrent();
            });
            trigger.AddClasses("ide-node__text-field-trigger");

            triggerField.Add(triggerLabel);
            triggerField.Add(trigger);

            extensionContainer.Add(triggerField);

            #endregion

            #region TIMEOUT

            var timeoutField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var timeoutLabel = IEGraphUtility.CreateLabel("Timeout(sc)");
            timeoutLabel.style.fontSize = 12;
            timeoutLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            timeoutLabel.style.paddingRight = 10f;
            timeoutLabel.style.minWidth = 90f;
            timeoutLabel.style.maxWidth = 90f;
            timeoutLabel.tooltip =
                "If the trigger is not activated within the specified time period, it will time out. If you do not want to apply a timeout, set the value to 0.";

            //TextField
            var timeout = IEGraphUtility.CreateFloatField(Timeout);
            timeout.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (timeout.value < 0) timeout.value = 0;
                if (Math.Abs(Timeout - timeout.value) < double.Epsilon) return;
                Timeout = timeout.value;
                GraphSaveUtility.SaveCurrent();
            });
            timeout.AddClasses("ide-node__float-field-trigger");
            timeout.style.marginLeft = 0;

            timeoutField.Add(timeoutLabel);
            timeoutField.Add(timeout);

            extensionContainer.Add(timeoutField);

            #endregion

            var schemePort =
                this.CreatePort("Scheme", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(schemePort);

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
                cPort.portColor = STATIC.BluePort;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}