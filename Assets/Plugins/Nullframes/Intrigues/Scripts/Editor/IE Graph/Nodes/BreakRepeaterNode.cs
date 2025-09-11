using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class BreakRepeaterNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Break_Repeater_Node";

        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }
        
        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-byzantine-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Break Repeater");
            titleLabel.AddClasses("ide-node__label__large");

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.BreakSequencerPort;
            inputContainer.Add(inputPort);

            foreach (var output in Outputs)
            {
                var cPort = this.CreatePort(output.Name, Port.Capacity.Multi);
                cPort.userData = output;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}