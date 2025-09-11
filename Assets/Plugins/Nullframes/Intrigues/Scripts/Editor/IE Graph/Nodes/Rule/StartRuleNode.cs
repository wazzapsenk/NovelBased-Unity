using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class StartRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Rule:Start_Node";

        public override bool IsMovable()
        {
            return false;
        }

        public override bool IsSelectable()
        {
            return false;
        }
        
        public override GenericNodeType GenericType => GenericNodeType.Rule;
        
        protected override void OnOutputInit()
        {
            AddOutput("Run");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-light-green-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Rule"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.GreenPort;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}