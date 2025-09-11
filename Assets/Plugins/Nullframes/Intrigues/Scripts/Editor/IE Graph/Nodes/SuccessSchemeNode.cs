using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SuccessSchemeNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Success_Scheme_Node";

        protected override void OnOutputInit()
        {
            AddOutput("Out");
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
                text = "Success Scheme"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputPort.portColor = STATIC.SuccessPort;
            
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