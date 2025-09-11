using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class KeyHandlerNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Key_Handler_Node";

        public KeyCode KeyCode;
        public KeyType KeyType;
        public int TapCount = 1;
        public float HoldTime;

        private FloatField timeField;
        private IntegerField tapField;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
            AddOutput("[Actor Is Not Player]");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-dark-green-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Key Handler"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var keyList = IEGraphUtility.CreateDropdown(null);
            keyList.style.minWidth = 120f;

            var keyNames = NullUtils._keyNames.ToList();

            keyList.choices = new List<string>(keyNames);

            var index = keyNames.IndexOf(KeyCode.ToString());
            keyList.index = index == -1 ? 0 : index;

            var dropdownChild = keyList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            keyList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                KeyCode = Enum.Parse<KeyCode>(keyNames.ElementAt(keyList.index));

                GraphSaveUtility.SaveCurrent();
            });
            keyList.style.marginLeft = 0f;
            keyList.style.marginBottom = 1f;
            keyList.style.marginTop = 1f;
            keyList.style.marginRight = 3f;
            keyList.AddClasses("ide-node__role-dropdown-field");
            
            //
            var keyType = IEGraphUtility.CreateDropdown(null);
            keyType.style.minWidth = 120f;

            // var keyTypes = Enum.GetNames(typeof(KeyType)).ToList();
            var keyTypes = new List<string>() { "Down", "Up" };

            keyType.choices = new List<string>(keyTypes);

            var typeIndex = keyTypes.IndexOf(KeyType.ToString());
            keyType.index = typeIndex == -1 ? 0 : typeIndex;

            var typeChild = keyType.GetChild<VisualElement>();
            typeChild.SetPadding(5);
            typeChild.style.paddingLeft = 10;
            typeChild.style.paddingRight = 10;
            typeChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            keyType.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                KeyType = Enum.Parse<KeyType>(keyTypes.ElementAt(keyType.index));

                Refresh();
                
                GraphSaveUtility.SaveCurrent();
            });
            keyType.style.marginLeft = 0f;
            keyType.style.marginBottom = 1f;
            keyType.style.marginTop = 1f;
            keyType.style.marginRight = 3f;
            keyType.AddClasses("ide-node__role-dropdown-field");
            
            //
            timeField = IEGraphUtility.CreateFloatField(HoldTime);

            timeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs(HoldTime - timeField.value) < double.Epsilon) return;
                HoldTime = timeField.value;
                GraphSaveUtility.SaveCurrent();
            });

            timeField.AddClasses("key_handler_float-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, keyList);
            titleContainer.Insert(2, keyType);
            titleContainer.Insert(3, timeField);
            
            //
            tapField = IEGraphUtility.CreateIntField(TapCount);

            tapField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (TapCount == tapField.value) return;
                if (TapCount < 1) {
                    tapField.value = 1;
                    return;
                }
                TapCount = tapField.value;
                GraphSaveUtility.SaveCurrent();
            });

            tapField.AddClasses("key_handler_int-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, keyList);
            titleContainer.Insert(2, keyType);
            titleContainer.Insert(3, timeField);
            titleContainer.Insert(4, tapField);

            var conspirator = this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(conspirator);
            var target = this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
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

            for (var i = 0; i < Outputs.Count; i++) {
                var outputData = Outputs[i];
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
                if (i == 1) {
                    cPort.portColor = STATIC.RedPort;
                    cPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                }
            }

            Refresh();
            
            RefreshExpandedState();
        }

        private void Refresh() {
            if (KeyType == KeyType.Hold) {
                timeField.Show();
            }
            else {
                timeField.Hide();
            }
            
            if (KeyType == KeyType.Tap) {
                tapField.Show();
            }
            else {
                tapField.Hide();
            }
        }
    }
}