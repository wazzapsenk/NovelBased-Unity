using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class IsSpouseNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Is_Spouse_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;
        
        protected override void OnOutputInit()
        {
            AddOutput("True");
            AddOutput("False");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-eggnog-node");
        }

        public override void Draw()
        {
            base.Draw();
            Debug.Log("test");
            var titleLabel = new Label()
            {
                text = "Is Spouse"
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(inputPort);
            inputPort.tooltip = "If the Conspirator and Target is spouse.";
            
            var dualPort = this.CreatePort("[Dual]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            dualPort.tooltip = "If the Primary Actor and Secondary Actor is spouse.";
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