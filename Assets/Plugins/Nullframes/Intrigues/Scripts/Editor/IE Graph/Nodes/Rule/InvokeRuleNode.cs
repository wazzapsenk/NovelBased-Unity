using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class InvokeRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Rule:Invoke_Node";

        private TextField methodNameField;
        public string MethodName = "Method Name";

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        protected override void OnOutputInit()
        {
            AddOutput("True");
            AddOutput("False");
            AddOutput("Null");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-cherry");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Invoke");
            titleLabel.AddClasses("ide-node__label");

            methodNameField = IEGraphUtility.CreateTextField(MethodName);

            methodNameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (MethodName == methodNameField.value) return;
                MethodName = methodNameField.value;
                GraphSaveUtility.SaveCurrent();
            });

            methodNameField.AddClasses("uis-invoke-text-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, methodNameField);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            for (var index = 0; index < Outputs.Count; index++) {
                var outputData = Outputs[index];
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.userData = outputData;
                
                cPort.portColor = index == 0 ? STATIC.GreenPort : index == 1 ? STATIC.BluePort : STATIC.RedPort;

                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}