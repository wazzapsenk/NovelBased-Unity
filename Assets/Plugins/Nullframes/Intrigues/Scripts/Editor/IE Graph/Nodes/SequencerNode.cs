using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SequencerNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Sequencer_Node";

        protected override void OnOutputInit()
        {
            AddOutput("-->");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-pink-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Sequencer");
            titleLabel.AddClasses("ide-node__label__large");

            tooltip = "The Sequencer node executes flows in order.\nThe order is from top to bottom.\n(The node at the top executes first.)";

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            foreach (var output in Outputs)
            {
                var cPort = this.CreatePort(output.Name, Port.Capacity.Multi);
                cPort.portType = typeof(int);
                cPort.portColor = STATIC.SequencerPort;
                cPort.userData = output;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}