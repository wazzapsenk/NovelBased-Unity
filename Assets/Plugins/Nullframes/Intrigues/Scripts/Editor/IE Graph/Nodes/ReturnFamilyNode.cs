using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ReturnFamilyNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Return_Family_Node";

        public string MethodName = "Method Name";
        
        protected override void OnOutputInit()
        {
            AddOutput("[Family]");
            AddOutput("[Null]");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-dark-brown");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Return Family"
            };

            titleLabel.AddClasses("ide-node__label");

            var methodNameField = IEGraphUtility.CreateTextField(MethodName);

            methodNameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (MethodName == methodNameField.value) return;
                MethodName = methodNameField.value;
                GraphSaveUtility.SaveCurrent();
            });

            methodNameField.AddClasses("uis-return-actor-text-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, methodNameField);

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

            RefreshExpandedState();
        }
    }
}