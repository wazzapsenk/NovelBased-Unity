using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class LogRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Log_Node";

        public string Message;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-dark-red-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Log");
            titleLabel.AddClasses("ide-node__label");

            var logField = IEGraphUtility.CreateTextField(Message);

            logField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Message == logField.value) return;
                Message = logField.value;
                GraphSaveUtility.SaveCurrent();
            });

            logField.AddClasses("ide-node__log-text-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, logField);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(inputPort);

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