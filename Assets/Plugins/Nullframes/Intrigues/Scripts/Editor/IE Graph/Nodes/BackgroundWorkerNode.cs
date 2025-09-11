using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;

namespace Nullframes.Intrigues.Graph.Nodes {
    public class BackgroundWorkerNode : INode {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Background_Node";

        protected override void OnOutputInit() {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView) {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-pink-node");
        }

        public override void Draw() {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Background Worker");
            titleLabel.AddClasses("ide-node__label__large");

            tooltip =
                "This node runs the subsequent nodes in the background.";

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.GreenPort;
            inputContainer.Add(inputPort);

            foreach (var output in Outputs) {
                var cPort = this.CreatePort(output.Name, Port.Capacity.Multi);
                cPort.portColor = NullUtils.HTMLColor("#00F5FF");
                cPort.userData = output;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}