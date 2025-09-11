using System.Collections.Generic;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SkipSequencerNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Skip_Sequencer_Node";

        public int Index;
        
        protected override void OnOutputInit()
        {
            AddOutput("Skip");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-pink-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Skip Sequencer");
            titleLabel.AddClasses("ide-node__label__large");
            
            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.FlowPort;
            inputContainer.Add(inputPort);
            
            var index = IEGraphUtility.CreateIntField(Index);
            
            index.AddClasses("uis-skip-index");

            index.RegisterCallback<FocusOutEvent>(_ =>
            {
                Index = index.value;
                GraphSaveUtility.SaveCurrent();
            });

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, index);
            
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