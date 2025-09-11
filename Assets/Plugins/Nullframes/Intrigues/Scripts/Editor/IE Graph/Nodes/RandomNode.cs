using UnityEditor.Experimental.GraphView;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RandomNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Random_Node";

        protected override void OnOutputInit()
        {
            AddOutput("Possibility");
        }
        
        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-mauve-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Random");
            titleLabel.AddClasses("ide-node__label__small");

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort("Out", null, Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}