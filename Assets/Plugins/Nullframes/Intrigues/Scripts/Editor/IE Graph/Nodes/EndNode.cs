using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class EndNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/End_Node";

        public override bool IsMovable()
        {
            return false;
        }

        public override bool IsSelectable()
        {
            return false;
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-red-node");
        }

        protected override void OnOutputInit()
        {
            AddOutput("Success");
            AddOutput("Failed");
            AddOutput("None");
            AddOutput("Any");
        }

        public override void Draw()
        {
            base.Draw();
            
            var titleLabel = new Label()
            {
                text = "End"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);
            
            var success = this.CreatePort(Outputs[0].Name, Port.Capacity.Multi);
            success.userData = Outputs[0];
            success.portColor = STATIC.SuccessPort;
            
            var failed = this.CreatePort(Outputs[1].Name, Port.Capacity.Multi);
            failed.userData = Outputs[1];
            failed.portColor = STATIC.RedPort;
            
            var none = this.CreatePort(Outputs[2].Name, Port.Capacity.Multi);
            none.userData = Outputs[2];
            none.portColor = STATIC.EndNode;
            
            var any = this.CreatePort(Outputs[3].Name, Port.Capacity.Multi);
            any.userData = Outputs[3];
            any.portColor = STATIC.SoftYellow;

            outputContainer.Add(any);
            outputContainer.Add(success);
            outputContainer.Add(failed);
            outputContainer.Add(none);
            
            RefreshExpandedState();
        }
    }
}