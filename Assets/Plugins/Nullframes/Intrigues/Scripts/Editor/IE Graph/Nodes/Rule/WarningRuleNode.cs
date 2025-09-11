using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class WarningRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Warning_Node";

        public string WarningName;
        public string Warning = "Warning";
        
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        protected override void OnOutputInit()
        {
            AddOutput("Next");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-jean-node");
            extensionContainer.AddClasses("warning-extension");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Warning");
            titleLabel.AddClasses("ide-node__label");

            var causeName = IEGraphUtility.CreateTextField(WarningName);
            
            causeName.AddClasses("uis-warning-name-field");
            
            causeName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(causeName.value))
                {
                    causeName.value = WarningName;
                    return;
                }

                var exists = graphView.graphElements.OfType<ErrorRuleNode>().Any(c => c.ErrorName == causeName.value) || graphView.graphElements.OfType<WarningRuleNode>().Any(c => c.WarningName == causeName.value);
                
                if (exists)
                {
                    causeName.value = WarningName;
                    return;
                }

                WarningName = causeName.value;
                GraphSaveUtility.SaveCurrent();
            });

            var causeField = IEGraphUtility.CreateTextArea(Warning);

            causeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Warning == causeField.value) return;
                Warning = causeField.value;
                GraphSaveUtility.SaveCurrent();
            });

            causeField.AddClasses("uis-warning-name-field");

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