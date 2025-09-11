using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SameClanNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Same_Clan_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;
        
        protected override void OnOutputInit()
        {
            AddOutput("True");
            AddOutput("False");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-fern-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Same Clan"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.tooltip = "Checks if Conspirator and Target are in the same clan.";
            inputContainer.Add(inputPort);
            
            var dualPort = this.CreatePort("[Dual]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            dualPort.tooltip = "Checks if Primary Actor and Secondary Actor are in the same clan.";
            dualPort.portColor = STATIC.GreenPort;
            dualPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(dualPort);

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