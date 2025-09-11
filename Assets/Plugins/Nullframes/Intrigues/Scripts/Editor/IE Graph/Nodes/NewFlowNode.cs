using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class NewFlowNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Flow_Node";

        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }
        
        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-fern-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Flow");
            titleLabel.AddClasses("ide-node__label__large");

            tooltip =
                "This node allows for independent progression within the flow.\nIt is unaffected by nodes like Sequencer and Repeater, which have control over the flow.";

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.RedPort;
            inputContainer.Add(inputPort);

            foreach (var output in Outputs)
            {
                var cPort = this.CreatePort(output.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.GreenPort;
                cPort.userData = output;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}