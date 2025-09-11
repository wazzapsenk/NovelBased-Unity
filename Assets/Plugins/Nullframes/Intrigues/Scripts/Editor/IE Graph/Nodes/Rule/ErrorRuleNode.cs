using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ErrorRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Error_Node";

        public string ErrorName;
        public string Error = "Error";
        
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        protected override void OnOutputInit()
        {
            AddOutput("Next");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-smoky-red");
            extensionContainer.AddClasses("cause-extension");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Error");
            titleLabel.AddClasses("ide-node__label");

            var causeName = IEGraphUtility.CreateTextField(ErrorName);
            
            causeName.AddClasses("uis-cause-name-field");
            
            causeName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(causeName.value))
                {
                    causeName.value = ErrorName;
                    return;
                }

                var exists = graphView.graphElements.OfType<ErrorRuleNode>().Any(c => c.ErrorName == causeName.value) || graphView.graphElements.OfType<WarningRuleNode>().Any(c => c.WarningName == causeName.value);

                if (exists)
                {
                    causeName.value = ErrorName;
                    return;
                }

                ErrorName = causeName.value;
                GraphSaveUtility.SaveCurrent();
            });

            var causeField = IEGraphUtility.CreateTextArea(Error);

            causeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Error == causeField.value) return;
                Error = causeField.value;
                GraphSaveUtility.SaveCurrent();
            });

            causeField.AddClasses("uis-cause-text-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, causeName);
            
            extensionContainer.Add(causeField);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.SequencerPort;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}