using System.Collections.Generic;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class DualActorNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Return_Dual_Actor_Node";

        public DualType DualType;
        public string MethodName = "Method Name";

        private TextField methodNameField;
        
        protected override void OnOutputInit()
        {
            AddOutput("[Dual]");
            AddOutput("[Null, Null]");
            AddOutput("[<color=#A3364C>Null</color>, <color=#67A365>Actor</color>]");
            AddOutput("[<color=#67A365>Actor</color>, <color=#A3364C>Null</color>]");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-dark-brown");
            mainContainer.style.minWidth = 250;
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Return Dual Actor"
            };

            titleLabel.AddClasses("ide-node__label");

            methodNameField = IEGraphUtility.CreateTextField(MethodName);

            methodNameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (MethodName == methodNameField.value) return;
                MethodName = methodNameField.value;
                GraphSaveUtility.SaveCurrent();
            });

            methodNameField.AddClasses("uis-return-actor-text-field");
            
            //Dropdown
            var dualType = IEGraphUtility.CreateDropdown(null);
            dualType.choices = new List<string> { "Primary: Conspirator \u25CF Secondary: Target", "Primary: Target \u25CF Secondary: Conspirator", "Get Actors with Attribute" };
            dualType.style.minWidth = 120f;

            dualType.index = (int)DualType;

            var dropdownChild = dualType.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            dualType.RegisterCallback<ChangeEvent<string>>(_ => {
                var dualT = (DualType)dualType.index;
                if (DualType == dualT) return;
                DualType = dualT;
                UpdateMethodField();
                GraphSaveUtility.SaveCurrent();
            });
            dualType.style.marginLeft = 0f;
            dualType.style.marginBottom = 1f;
            dualType.style.marginTop = 1f;
            dualType.style.marginRight = 3f;
            dualType.AddClasses("ide-node__actor-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, methodNameField);
            
            mainContainer.Add(dualType);

            var input = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(input);

            for (var j = 0; j < Outputs.Count; j++)
            {
                var outputData = Outputs[j];
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                if (j == 0)
                {
                    cPort.portColor = STATIC.GreenPort;
                    cPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                }
                else
                {
                    cPort.portColor = STATIC.RedPort;
                    cPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic);
                }
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            UpdateMethodField();

            RefreshExpandedState();
        }

        private void UpdateMethodField() {
            if (DualType == DualType.GetActors) {
                methodNameField.Show();
            }
            else {
                methodNameField.Hide();
            }
        }
    }
}