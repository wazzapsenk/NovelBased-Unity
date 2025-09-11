using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class OnLoadNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/On_Load_Node";

        public override bool IsCopiable() => false;

        protected override void OnOutputInit()
        {
            AddOutput("Load");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-jean-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "On Load"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

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